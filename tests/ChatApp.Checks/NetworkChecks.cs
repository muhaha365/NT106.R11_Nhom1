using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using ChatApp.Client;
using ChatApp.Shared;
using ChatApp.Shared.Contracts;

namespace ChatApp.Checks;

internal static class NetworkChecks
{
    public static async Task RunAsync()
    {
        await CorrelatesResponsesAndEvents();
        await LegacyChatRemainsCompatible();
        await ReturnsServerErrors();
        await AuthenticationFailureInvalidatesSession();
        await CancellationDoesNotConsumeNextResponse();
        await TimeoutDoesNotRetry();
        await DisconnectCompletesPendingRequests();
        await ReconnectDoesNotKeepOldSession();
    }

    private static async Task CorrelatesResponsesAndEvents()
    {
        using var client = new ProtocolClient();
        using var peer = await LoopbackPeer.ConnectAsync(client);
        var received = new TaskCompletionSource<Packet>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.EventReceived += p => received.TrySetResult(p);
        var first = client.RequestAsync<UserIdResult>("friend.remove.request", new { UserId = 2 });
        var second = client.RequestAsync<UserIdResult>("friend.remove.request", new { UserId = 3 });
        var a = await peer.ReadAsync();
        var b = await peer.ReadAsync();
        Check(a.RequestId != b.RequestId && Guid.TryParse(a.RequestId, out _), "Unique GUID request IDs");
        await peer.SendAsync(new Packet { Type = "presence.changed.event", Payload = JsonSerializer.SerializeToElement(new { UserId = 4, IsOnline = true }) });
        await peer.ReplyAsync(b, new { UserId = 3 });
        await peer.ReplyAsync(a, new { UserId = 2 });
        Check((await first).UserId == 2 && (await second).UserId == 3, "Out-of-order correlation");
        Check((await received.Task.WaitAsync(TimeSpan.FromSeconds(3))).Type == "presence.changed.event", "Event dispatch");
    }

    private static async Task LegacyChatRemainsCompatible()
    {
        using var client = new ProtocolClient();
        using var peer = await LoopbackPeer.ConnectAsync(client);
        var received = new TaskCompletionSource<Packet>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.EventReceived += packet => received.TrySetResult(packet);
        await client.SendLegacyChatAsync("Xin chào nhóm!\nDòng thứ hai.");
        var sent = await peer.ReadAsync();
        Check(sent.Type == "chat" && sent.RequestId is null && sent.Payload!.Value.GetString() == "Xin chào nhóm!\nDòng thứ hai.", "Demo sends a string payload");
        await peer.SendAsync(new Packet { Type = "chat", Payload = JsonSerializer.SerializeToElement("Client 2 gửi: Xin chào") });
        var reply = await received.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Check(reply.Type == "chat" && reply.Payload!.Value.GetString() == "Client 2 gửi: Xin chào", "Demo dispatches broadcasts without a RequestId");
    }

    private static async Task ReturnsServerErrors()
    {
        using var client = new ProtocolClient();
        using var peer = await LoopbackPeer.ConnectAsync(client);
        var request = client.RequestAsync<UserIdResult>("friend.remove.request", new { UserId = 2 });
        var packet = await peer.ReadAsync();
        await peer.SendAsync(new Packet { Type = "friend.remove.response", RequestId = packet.RequestId,
            Payload = JsonSerializer.SerializeToElement(new { Success = false, Data = (object?)null, Error = new { Code = "FORBIDDEN", Message = "Không đủ quyền." } }) });
        var error = await ThrowsAsync<ProtocolRequestException>(request);
        Check(error.Code == "FORBIDDEN", "Business error code");

        request = client.RequestAsync<UserIdResult>("friend.remove.request", new { UserId = 2 });
        packet = await peer.ReadAsync();
        await peer.SendAsync(new Packet { Type = "system.error.response", RequestId = packet.RequestId,
            Payload = JsonSerializer.SerializeToElement(new { Code = "UNKNOWN_TYPE", Message = "Chưa hỗ trợ." }) });
        error = await ThrowsAsync<ProtocolRequestException>(request);
        Check(error.Code == "UNKNOWN_TYPE", "Protocol error code");
    }

    private static async Task AuthenticationFailureInvalidatesSession()
    {
        using var client = new ProtocolClient();
        using var peer = await LoopbackPeer.ConnectAsync(client);
        var request = client.RequestAsync<UserIdResult>("friend.remove.request", new { UserId = 2 });
        var packet = await peer.ReadAsync();
        await peer.SendAsync(new Packet { Type = "friend.remove.response", RequestId = packet.RequestId,
            Payload = JsonSerializer.SerializeToElement(new { Success = false, Data = (object?)null, Error = new { Code = "NOT_AUTHENTICATED", Message = "Bạn cần đăng nhập." } }) });
        var error = await ThrowsAsync<ProtocolRequestException>(request);
        Check(error.Code == "NOT_AUTHENTICATED" && !client.IsConnected, "Expired authentication invalidates the shared session");
    }

    private static async Task CancellationDoesNotConsumeNextResponse()
    {
        using var client = new ProtocolClient();
        using var peer = await LoopbackPeer.ConnectAsync(client);
        using var cancellation = new CancellationTokenSource();
        var first = client.RequestAsync<UserIdResult>("friend.remove.request", new { UserId = 2 }, cancellation.Token);
        var old = await peer.ReadAsync();
        cancellation.Cancel();
        await ThrowsAsync<OperationCanceledException>(first);
        var second = client.RequestAsync<UserIdResult>("friend.remove.request", new { UserId = 3 });
        var current = await peer.ReadAsync();
        await peer.ReplyAsync(old, new { UserId = 2 });
        await peer.ReplyAsync(current, new { UserId = 3 });
        Check((await second).UserId == 3 && client.IsConnected, "Late response ignored without disconnect");
    }

    private static async Task TimeoutDoesNotRetry()
    {
        using var client = new ProtocolClient(TimeSpan.FromMilliseconds(350));
        using var peer = await LoopbackPeer.ConnectAsync(client);
        var request = client.RequestAsync<UserIdResult>("friend.remove.request", new { UserId = 2 });
        await peer.ReadAsync();
        await ThrowsAsync<TimeoutException>(request);
        Check(peer.AvailableBytes == 0, "Mutation was not retried");
    }

    private static async Task DisconnectCompletesPendingRequests()
    {
        using var client = new ProtocolClient();
        using var peer = await LoopbackPeer.ConnectAsync(client);
        var disconnected = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Disconnected += reason => disconnected.TrySetResult(reason);
        var request = client.RequestAsync<UserIdResult>("friend.remove.request", new { UserId = 2 });
        await peer.ReadAsync();
        peer.CloseConnection();
        await ThrowsAsync<IOException>(request);
        await disconnected.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Check(!client.IsConnected, "Disconnected state");
    }

    private static async Task ReconnectDoesNotKeepOldSession()
    {
        using var client = new ProtocolClient();
        using var firstPeer = await LoopbackPeer.ConnectAsync(client);
        var pending = client.RequestAsync<UserIdResult>("friend.remove.request", new { UserId = 2 });
        await firstPeer.ReadAsync();
        client.Disconnect();
        await ThrowsAsync<IOException>(pending);
        using var newPeer = await LoopbackPeer.ConnectAsync(client);
        var request = client.RequestAsync<UserIdResult>("friend.remove.request", new { UserId = 3 });
        var packet = await newPeer.ReadAsync();
        await newPeer.ReplyAsync(packet, new { UserId = 3 });
        Check((await request).UserId == 3 && client.IsConnected, "New session survived old cleanup");
    }

    internal static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + message);
    }

    internal static async Task<T> ThrowsAsync<T>(Task task) where T : Exception
    {
        try { await task.WaitAsync(TimeSpan.FromSeconds(4)); }
        catch (T error) { return error; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}

// Test-only TCP peer. It returns explicit fixture packets, not application business data.
internal sealed class LoopbackPeer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly TcpClient _socket;
    private readonly NetworkStream _stream;
    public int AvailableBytes => _socket.Available;

    private LoopbackPeer(TcpListener listener, TcpClient socket)
    {
        _listener = listener;
        _socket = socket;
        _stream = socket.GetStream();
    }

    public static async Task<LoopbackPeer> ConnectAsync(ProtocolClient client)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var accepted = listener.AcceptTcpClientAsync();
        await client.ConnectAsync("127.0.0.1", ((IPEndPoint)listener.LocalEndpoint).Port);
        return new LoopbackPeer(listener, await accepted);
    }

    public Task<Packet> ReadAsync() => TestWire.ReadAsync(_stream).WaitAsync(TimeSpan.FromSeconds(3));

    public Task SendAsync(Packet packet) => TestWire.SendAsync(_stream, packet);
    public Task ReplyAsync(Packet request, object data) => SendAsync(new Packet {
        Type = request.Type.Replace(".request", ".response"), RequestId = request.RequestId,
        Payload = JsonSerializer.SerializeToElement(new { Success = true, Data = data, Error = (object?)null }) });
    public void CloseConnection() => _socket.Dispose();
    public void Dispose() { _socket.Dispose(); _listener.Stop(); }
}

// Independent fixture framing: 4-byte big-endian byte length, then UTF-8 JSON.
internal static class TestWire
{
    public static async Task<Packet> ReadAsync(Stream stream)
    {
        var header = new byte[4];
        await stream.ReadExactlyAsync(header);
        var length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length <= 0 || length > 1_048_576) throw new InvalidDataException("Invalid fixture frame length");
        var payload = new byte[length];
        await stream.ReadExactlyAsync(payload);
        return JsonSerializer.Deserialize<Packet>(payload) ?? throw new InvalidDataException("Missing fixture packet");
    }

    public static async Task SendAsync(Stream stream, Packet packet, JsonSerializerOptions? options = null)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(packet, options);
        var header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, payload.Length);
        await stream.WriteAsync(header);
        await stream.WriteAsync(payload);
    }
}
