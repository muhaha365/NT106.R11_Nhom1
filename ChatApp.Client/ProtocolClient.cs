using System.Collections.Concurrent;
using System.Text.Json;
using ChatApp.Shared;

namespace ChatApp.Client;

// Gửi request, chờ đúng response và chuyển event từ server cho UI.
public sealed class ProtocolClient : IDisposable
{
    private sealed record Pending(ClientConnection Connection, string ResponseType, TaskCompletionSource<Packet> Completion);
    private sealed record ServerReply(bool Success, JsonElement Data, ServerError? Error);
    private sealed record ServerError(string Code, string Message);
    private readonly ConcurrentDictionary<string, Pending> _pending = new();
    private readonly SemaphoreSlim _connectGate = new(1, 1);
    private readonly JsonSerializerOptions _json = new() { RespectRequiredConstructorParameters = true, RespectNullableAnnotations = true };
    private readonly TimeSpan _timeout;
    private ClientConnection? _connection;
    private bool _disposed;

    public bool IsConnected => _connection is not null;
    public event Action<Packet>? EventReceived;
    public event Action<string>? Disconnected;

    public ProtocolClient(TimeSpan? requestTimeout = null)
    {
        _timeout = requestTimeout ?? TimeSpan.FromSeconds(10);
        if (_timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(requestTimeout));
    }

    public async Task ConnectAsync(string host, int port)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _connectGate.WaitAsync().ConfigureAwait(false);
        var connection = new ClientConnection();
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            Disconnect();
            await connection.ConnectAsync(host, port).WaitAsync(_timeout).ConfigureAwait(false);
            ObjectDisposedException.ThrowIf(_disposed, this);
            _connection = connection;
            _ = ReceiveLoopAsync(connection);
        }
        finally
        {
            if (_connection != connection) connection.Dispose(); // Dọn kết nối nếu mở thất bại hoặc đã bị đóng.
            _connectGate.Release();
        }
    }

    public async Task<T> RequestAsync<T>(string requestType, object payload, CancellationToken cancellationToken = default) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!requestType.EndsWith(".request", StringComparison.Ordinal)) throw new ArgumentException("Loại gói phải là request.", nameof(requestType));
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();
        var connection = _connection ?? throw new InvalidOperationException("Chưa kết nối tới server.");
        var id = Guid.NewGuid().ToString();
        var request = new Pending(connection, requestType[..^8] + ".response", new(TaskCreationOptions.RunContinuationsAsynchronously));
        _pending[id] = request; // Lưu chỗ chờ trước khi gửi để không bỏ lỡ phản hồi nhanh.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        try
        {
            if (_connection != connection) throw new IOException("Kết nối đã đóng trước khi gửi yêu cầu.");
            var packet = new Packet { Type = requestType, RequestId = id, Payload = JsonSerializer.SerializeToElement(payload) };
            await connection.SendAsync(packet).WaitAsync(timeout.Token).ConfigureAwait(false);
            var response = await request.Completion.Task.WaitAsync(timeout.Token).ConfigureAwait(false);
            return ReadReply<T>(response, connection);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Chưa nhận được phản hồi. Kết quả thao tác chưa xác định; hãy tải lại dữ liệu trước khi thử lại.");
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidDataException)
        {
            Close(connection, "Server gửi phản hồi không hợp lệ.");
            throw new IOException("Phản hồi của server không đúng protocol.", error);
        }
        finally { _pending.TryRemove(id, out _); }
    }

    private T ReadReply<T>(Packet response, ClientConnection connection) where T : class
    {
        var body = response.Payload ?? throw new JsonException("Phản hồi thiếu Payload.");
        if (response.Type == "system.error.response")
        {
            var error = body.Deserialize<ServerError>(_json) ?? throw new JsonException("Phản hồi thiếu lỗi.");
            throw new ProtocolRequestException(error.Code, error.Message);
        }
        var reply = body.Deserialize<ServerReply>(_json) ?? throw new JsonException("Phản hồi rỗng.");
        if (reply.Success)
        {
            if (reply.Error is not null || reply.Data.ValueKind != JsonValueKind.Object) throw new JsonException("Dữ liệu thành công không hợp lệ.");
            return reply.Data.Deserialize<T>(_json) ?? throw new JsonException("Phản hồi thiếu dữ liệu.");
        }
        if (reply.Data.ValueKind != JsonValueKind.Null || reply.Error is null) throw new JsonException("Dữ liệu lỗi không hợp lệ.");
        if (reply.Error.Code == "NOT_AUTHENTICATED") Close(connection, "Phiên đăng nhập không còn hợp lệ. Hãy đăng nhập lại.");
        throw new ProtocolRequestException(reply.Error.Code, reply.Error.Message);
    }

    private async Task ReceiveLoopAsync(ClientConnection connection)
    {
        var reason = "Server đã ngắt kết nối.";
        try
        {
            while (_connection == connection)
            {
                var packet = await connection.ReceiveAsync().ConfigureAwait(false);
                if (packet is null || _connection != connection) break;
                if (packet.Version != 1 || string.IsNullOrWhiteSpace(packet.Type)) throw new InvalidDataException();
                if (packet.RequestId is null) // Event không có RequestId; UI tự xử lý nội dung.
                {
                    if (packet.Type.EndsWith(".response", StringComparison.Ordinal)) throw new InvalidDataException();
                    EventReceived?.Invoke(packet);
                    continue;
                }
                if (packet.Type.EndsWith(".event", StringComparison.Ordinal)) throw new InvalidDataException();
                if (!_pending.TryGetValue(packet.RequestId, out var request) || request.Connection != connection) continue;
                if (packet.Type != request.ResponseType && packet.Type != "system.error.response") throw new InvalidDataException();
                request.Completion.TrySetResult(packet); // Đánh thức đúng request đang chờ.
            }
        }
        catch (Exception error) when (error is IOException or System.Net.Sockets.SocketException or ObjectDisposedException or JsonException or InvalidDataException)
        {
            reason = "Kết nối tới server đã đóng hoặc dữ liệu nhận không hợp lệ.";
        }
        finally { Close(connection, reason); }
    }

    public Task SendLegacyChatAsync(string content)
    {
        var connection = _connection ?? throw new InvalidOperationException("Chưa kết nối tới server.");
        return connection.SendAsync(new Packet { Type = "chat", Payload = JsonSerializer.SerializeToElement(content) });
    }

    private void Close(ClientConnection connection, string reason)
    {
        // Chỉ đóng phiên hiện tại một lần; vòng nhận cũ không được đóng phiên mới.
        if (Interlocked.CompareExchange(ref _connection, null, connection) != connection) return;
        connection.Dispose();
        foreach (var entry in _pending)
            if (entry.Value.Connection == connection && _pending.TryRemove(entry.Key, out var request))
                request.Completion.TrySetException(new IOException(reason));
        if (_connection is null) Disconnected?.Invoke(reason);
    }

    public void Disconnect() { if (_connection is { } connection) Close(connection, "Đã ngắt kết nối."); }
    public void Dispose() { _disposed = true; Disconnect(); }
}

public sealed class ProtocolRequestException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
