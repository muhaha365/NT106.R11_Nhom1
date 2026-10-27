using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatApp.Client;
using ChatApp.Shared;
using ChatApp.Shared.Contracts;

namespace ChatApp.Checks;

// Chỉ dành cho kiểm thử UI/client: trả dữ liệu fixture qua TCP, không được dùng làm server ứng dụng.
internal sealed class FixtureServer : IDisposable
{
    public static readonly UserSummary Self = new(1, "leanh", "Lê Anh", true);
    private static readonly DateTimeOffset Time = new(2026, 10, 27, 3, 0, 0, TimeSpan.Zero);
    private static readonly UserSummary Bao = new(2, "baochuong", "Bảo Chương", true);
    private static readonly UserSummary Long = new(3, "long", "Long", false);
    private static readonly UserSummary Minh = new(4, "minh", "Minh", true);
    private static readonly UserSummary Ha = new(5, "ha", "Hà", false);
    private static readonly UserSummary Ngoc = new(6, "ngoc", "Ngọc Anh", true);
    public static readonly GroupSummary FirstGroup = new(10, "Nhóm đồ án NT106", 1, Time);
    private static readonly GroupSummary SecondGroup = new(20, "Thảo luận bài lab", 2, Time);
    private readonly TcpListener _listener;
    private readonly TcpClient _socket;
    private readonly NetworkStream _stream;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly JsonSerializerOptions _json = new() { Converters = { new UtcConverter() } };
    private readonly ConcurrentQueue<Packet> _requests = new();
    private readonly List<UserSummary> _accepted = [Bao, Long];
    private readonly List<UserSummary> _members = [Self, Bao, Long];
    private readonly List<GroupSummary> _groups = [FirstGroup, SecondGroup];
    private readonly List<MessageSummary> _sentMessages = [];
    private bool _incomingAccepted, _outgoingAdded, _disposed;
    private bool _holdHistory;
    private TaskCompletionSource<Packet> _held = NewSignal();
    private int _messageId = 300;
    public string? DropNextReplyType { get; set; }
    public IReadOnlyList<Packet> Requests => _requests.ToArray();

    private static TaskCompletionSource<Packet> NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private FixtureServer(TcpListener listener, TcpClient socket)
    {
        _listener = listener;
        _socket = socket;
        _stream = socket.GetStream();
        _ = ReadLoopAsync();
    }

    public static async Task<FixtureServer> ConnectAsync(ProtocolClient client)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var accepted = listener.AcceptTcpClientAsync();
        await client.ConnectAsync("127.0.0.1", ((IPEndPoint)listener.LocalEndpoint).Port);
        return new FixtureServer(listener, await accepted);
    }

    public void HoldNextHistory() { _held = NewSignal(); _holdHistory = true; }
    public Task<Packet> HeldHistoryAsync() => _held.Task.WaitAsync(System.TimeSpan.FromSeconds(5));
    public async Task CompleteHistoryAsync()
    {
        var request = await HeldHistoryAsync();
        await ReplyAsync(request, History(10, null));
    }
    public Task SendMessageEventAsync(MessageSummary message) => SendAsync(new Packet {
        Type = "chat.message.event", Payload = JsonSerializer.SerializeToElement(new { Message = message }, _json) });
    public Task SendCreatedGroupAsync(GroupSummary group) => SendAsync(new Packet {
        Type = "group.changed.event", Payload = JsonSerializer.SerializeToElement(new { GroupId = group.Id, Action = "created", UserId = (int?)null, Group = group }, _json) });
    public static MessageSummary Message(int id, int group, string content, int sender = 2) => new(id, sender, null, group, content, Time);

    public async Task RemoveSelfAsync()
    {
        _groups.RemoveAll(group => group.Id == 10);
        await SendAsync(new Packet { Type = "group.changed.event", Payload = JsonSerializer.SerializeToElement(new {
            GroupId = 10, Action = "member_removed", UserId = 1, Group = FirstGroup }, _json) });
    }

    private async Task ReadLoopAsync()
    {
        try
        {
            while (!_disposed)
            {
                var packet = await TestWire.ReadAsync(_stream);
                _requests.Enqueue(packet);
                if (packet.Type == "chat")
                {
                    await SendAsync(new Packet { Type = "chat", Payload = JsonSerializer.SerializeToElement("Phản hồi demo") });
                    continue;
                }
                if (packet.Type == "chat.history.request" && _holdHistory && packet.Payload!.Value.GetProperty("GroupId").GetInt32() == 10)
                { _holdHistory = false; _held.TrySetResult(packet); continue; }
                var data = DataFor(packet);
                if (DropNextReplyType == packet.Type) { DropNextReplyType = null; continue; }
                await ReplyAsync(packet, data);
            }
        }
        catch (Exception error) when (error is IOException or ObjectDisposedException or SocketException) { }
    }

    private object DataFor(Packet packet)
    {
        var data = packet.Payload!.Value;
        int? after = data.TryGetProperty("AfterId", out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : null;
        switch (packet.Type)
        {
            case "friend.list.request":
                if (data.GetProperty("Status").GetString() == "Accepted")
                {
                    var people = _accepted.Where(user => user.Id > (after ?? 0)).OrderBy(user => user.Id).ToArray();
                    return new PageResult<FriendEntry>(people.Take(1).Select(user => Entry(user, "Accepted")).ToArray(), people.Length > 1 ? people[0].Id : null);
                }
                var pending = new List<FriendEntry> { new(Ha, new FriendshipSummary(1, 5, "Pending", Time)) };
                if (!_incomingAccepted) pending.Add(new(Minh, new FriendshipSummary(4, 1, "Pending", Time)));
                if (_outgoingAdded) pending.Add(Entry(Ngoc, "Pending"));
                return new PageResult<FriendEntry>(pending.OrderBy(item => item.User.Id).ToArray(), null);
            case "user.search.request": return new PageResult<UserSummary>([Ngoc], null);
            case "friend.add.request": _outgoingAdded = true; return new FriendshipResult(Entry(Ngoc, "Pending").Friendship);
            case "friend.respond.request":
                _incomingAccepted = true;
                if (data.GetProperty("Accept").GetBoolean()) { _accepted.Add(Minh); return new FriendshipResult(new(4, 1, "Accepted", Time)); }
                return new FriendshipResult(null);
            case "group.list.request":
                var groups = _groups.Where(group => group.Id > (after ?? 0)).OrderBy(group => group.Id).ToArray();
                return new PageResult<GroupSummary>(groups.Take(1).ToArray(), groups.Length > 1 ? groups[0].Id : null);
            case "group.members.request": return new PageResult<UserSummary>(_members.ToArray(), null);
            case "group.create.request":
                var created = new GroupSummary(30, data.GetProperty("GroupName").GetString()!, 1, Time);
                _groups.Add(created);
                return new GroupResult(created);
            case "group.member.add.request":
                _members.Add(Minh);
                return new GroupMemberResult(data.GetProperty("GroupId").GetInt32(), data.GetProperty("UserId").GetInt32());
            case "chat.history.request":
                int? before = data.GetProperty("BeforeId").ValueKind == JsonValueKind.Number ? data.GetProperty("BeforeId").GetInt32() : null;
                return History(data.GetProperty("GroupId").GetInt32(), before);
            case "chat.send.request":
                var message = Message(Interlocked.Increment(ref _messageId), data.GetProperty("GroupId").GetInt32(), data.GetProperty("Content").GetString()!, 1);
                _sentMessages.Add(message);
                return new MessageResult(message);
            default: throw new InvalidDataException("Unexpected fixture request: " + packet.Type);
        }
    }

    private static FriendEntry Entry(UserSummary user, string status) => new(user, new(1, user.Id, status, Time));
    private HistoryResult History(int group, int? before)
    {
        HistoryResult initial = group != 10
            ? new([Message(201, group, "Tin nhắn của nhóm bài lab.")], null)
            : before is null
                ? new([Message(101, 10, "Chào nhóm, cùng chốt kế hoạch nhé."), Message(102, 10, "Tin đến trong lúc tải lịch sử.")], 101)
                : new([Message(100, 10, "Tin nhắn cũ của nhóm.", 3)], null);
        return initial with { Items = initial.Items.Concat(_sentMessages.Where(m => m.GroupId == group && (before is null || m.Id < before))).OrderBy(m => m.Id).ToArray() };
    }

    private Task ReplyAsync(Packet request, object data) => SendAsync(new Packet {
        Type = request.Type.Replace(".request", ".response"), RequestId = request.RequestId,
        Payload = JsonSerializer.SerializeToElement(new { Success = true, Data = data, Error = (object?)null }, _json) });

    public async Task SendAsync(Packet packet)
    {
        await _sendLock.WaitAsync();
        try { await TestWire.SendAsync(_stream, packet, _json); }
        finally { _sendLock.Release(); }
    }

    public void Dispose() { _disposed = true; _socket.Dispose(); _listener.Stop(); }

    private sealed class UtcConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.GetDateTimeOffset();
        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) => writer.WriteStringValue(value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"));
    }
}
