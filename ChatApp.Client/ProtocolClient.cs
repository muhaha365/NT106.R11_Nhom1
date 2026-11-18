using System.Collections.Concurrent;
using System.Text.Json;
using ChatApp.Shared;

namespace ChatApp.Client;

// Gửi request, chờ đúng response và chuyển event từ server cho UI.
public sealed class ProtocolClient : IDisposable
{
    // Một yêu cầu đang chờ: gửi trên kết nối nào, đợi loại phản hồi nào.
    private sealed class Pending
    {
        public ClientConnection Connection { get; }
        public string ResponseType { get; }
        public TaskCompletionSource<Packet> Completion { get; }

        public Pending(ClientConnection connection, string responseType, TaskCompletionSource<Packet> completion)
        {
            Connection = connection;
            ResponseType = responseType;
            Completion = completion;
        }
    }

    // Hai record này chỉ mô tả các trường JSON server gửi về.
    private sealed record ServerReply(bool Success, JsonElement Data, ServerError? Error);
    private sealed record ServerError(string Code, string Message);
    // Dictionary dùng được từ cả nơi gửi và vòng nhận chạy nền; khóa tra cứu là RequestId.
    private readonly ConcurrentDictionary<string, Pending> _pending = new();
    private readonly SemaphoreSlim _connectGate = new(1, 1);
    private readonly JsonSerializerOptions _json = new()
    {
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true
    };
    private readonly TimeSpan _timeout;
    private ClientConnection? _connection;
    private bool _disposed;

    public bool IsConnected => _connection is not null;
    public event Action<Packet>? EventReceived;
    public event Action<string>? Disconnected;

    public ProtocolClient(TimeSpan? requestTimeout = null)
    {
        _timeout = requestTimeout ?? TimeSpan.FromSeconds(10);
        if (_timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(requestTimeout));
        }
    }

    public async Task ConnectAsync(string host, int port)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // Chỉ mở một kết nối tại một thời điểm. ConfigureAwait(false) cho phép tiếp tục ở luồng nền.
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
            // Dọn kết nối nếu mở thất bại hoặc đã bị đóng.
            if (_connection != connection) connection.Dispose();
            _connectGate.Release();
        }
    }

    public async Task<T> RequestAsync<T>(string requestType, object payload, CancellationToken cancellationToken = default) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!requestType.EndsWith(".request", StringComparison.Ordinal))
        {
            throw new ArgumentException("Loại gói phải là request.", nameof(requestType));
        }
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();
        var connection = _connection;
        if (connection == null)
        {
            throw new InvalidOperationException("Chưa kết nối tới server.");
        }
        var id = Guid.NewGuid().ToString();
        string requestName = requestType.Substring(0, requestType.Length - ".request".Length);
        string responseType = requestName + ".response";
        // TaskCompletionSource là "chỗ chờ": await ở đây, vòng nhận điền kết quả ở TrySetResult.
        var completion = new TaskCompletionSource<Packet>(TaskCreationOptions.RunContinuationsAsynchronously);
        var request = new Pending(connection, responseType, completion);
        _pending[id] = request; // Lưu chỗ chờ trước khi gửi để không bỏ lỡ phản hồi nhanh.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        try
        {
            if (_connection != connection)
            {
                throw new IOException("Kết nối đã đóng trước khi gửi yêu cầu.");
            }
            var packet = new Packet
            {
                Type = requestType,
                RequestId = id,
                Payload = JsonSerializer.SerializeToElement(payload)
            };
            await connection.SendAsync(packet).WaitAsync(timeout.Token).ConfigureAwait(false);
            var response = await request.Completion.Task.WaitAsync(timeout.Token).ConfigureAwait(false);
            return ReadReply<T>(response, connection);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Chờ quá 10 giây (mặc định). UI bắt lỗi này và yêu cầu tải lại trước khi thử lại.
            throw new TimeoutException("Chưa nhận được phản hồi. Kết quả thao tác chưa xác định; hãy tải lại dữ liệu trước khi thử lại.");
        }
        catch (Exception error) when (error is JsonException || error is KeyNotFoundException || error is InvalidDataException)
        {
            Close(connection, "Server gửi phản hồi không hợp lệ.");
            throw new IOException("Phản hồi của server không đúng protocol.", error);
        }
        finally
        {
            _pending.TryRemove(id, out _);
        }
    }

    private T ReadReply<T>(Packet response, ClientConnection connection) where T : class
    {
        if (!response.Payload.HasValue)
            throw new JsonException("Phản hồi thiếu Payload.");
        JsonElement body = response.Payload.Value;
        if (response.Type == "system.error.response")
        {
            ServerError error = ReadData<ServerError>(body, "Phản hồi thiếu lỗi.");
            throw new ProtocolRequestException(error.Code, error.Message);
        }
        ServerReply reply = ReadData<ServerReply>(body, "Phản hồi rỗng.");
        if (reply.Success)
        {
            if (reply.Error != null || reply.Data.ValueKind != JsonValueKind.Object)
                throw new JsonException("Dữ liệu thành công không hợp lệ.");
            return ReadData<T>(reply.Data, "Phản hồi thiếu dữ liệu.");
        }
        if (reply.Data.ValueKind != JsonValueKind.Null || reply.Error == null)
            throw new JsonException("Dữ liệu lỗi không hợp lệ.");
        if (reply.Error.Code == "NOT_AUTHENTICATED")
        {
            Close(connection, "Phiên đăng nhập không còn hợp lệ. Hãy đăng nhập lại.");
        }
        throw new ProtocolRequestException(reply.Error.Code, reply.Error.Message);
    }

    private T ReadData<T>(JsonElement json, string errorMessage) where T : class
    {
        T? data = json.Deserialize<T>(_json);
        if (data == null) throw new JsonException(errorMessage);
        return data;
    }

    private async Task ReceiveLoopAsync(ClientConnection connection)
    {
        var reason = "Server đã ngắt kết nối.";
        try
        {
            while (_connection == connection)
            {
                var packet = await connection.ReceiveAsync().ConfigureAwait(false);
                if (packet == null || _connection != connection) break;
                if (!DispatchPacket(packet, connection))
                {
                    reason = "Server gửi gói tin không đúng protocol.";
                    break;
                }
            }
        }
        catch (Exception error) when (error is IOException || error is System.Net.Sockets.SocketException
            || error is ObjectDisposedException || error is JsonException || error is InvalidDataException)
        {
            reason = "Kết nối tới server đã đóng hoặc dữ liệu nhận không hợp lệ.";
        }
        finally
        {
            Close(connection, reason);
        }
    }

    // Trả false khi gói sai để vòng nhận đóng kết nối; không cần throw rồi catch ngay.
    private bool DispatchPacket(Packet packet, ClientConnection connection)
    {
        if (packet.Version != 1 || string.IsNullOrWhiteSpace(packet.Type)) return false;
        if (packet.RequestId == null)
        {
            if (packet.Type.EndsWith(".response", StringComparison.Ordinal)) return false;
            EventReceived?.Invoke(packet); // Event/chat không chờ request; chuyển thẳng cho MainForm.
            return true;
        }

        if (packet.Type.EndsWith(".event", StringComparison.Ordinal)) return false;
        // Phản hồi đến sau khi request hết thời gian chờ thì bỏ qua.
        if (!_pending.TryGetValue(packet.RequestId, out var request)) return true;
        if (request.Connection != connection) return true;
        if (packet.Type != request.ResponseType && packet.Type != "system.error.response") return false;
        request.Completion.TrySetResult(packet);
        return true;
    }

    public Task SendLegacyChatAsync(string content)
    {
        ClientConnection? connection = _connection;
        if (connection == null)
        {
            throw new InvalidOperationException("Chưa kết nối tới server.");
        }
        Packet packet = new Packet
        {
            Type = "chat",
            Payload = JsonSerializer.SerializeToElement(content)
        };
        return connection.SendAsync(packet);
    }

    private void Close(ClientConnection connection, string reason)
    {
        // Chỉ đặt _connection = null nếu nó vẫn là connection này, trong một thao tác an toàn giữa các luồng.
        // Ví dụ: vừa kết nối lại thì vòng nhận của kết nối cũ không được đóng kết nối mới.
        if (Interlocked.CompareExchange(ref _connection, null, connection) != connection) return;
        connection.Dispose();
        foreach (var entry in _pending)
        {
            if (entry.Value.Connection == connection && _pending.TryRemove(entry.Key, out var request))
            {
                request.Completion.TrySetException(new IOException(reason));
            }
        }
        if (_connection == null) Disconnected?.Invoke(reason);
    }

    public void Disconnect()
    {
        var connection = _connection;
        if (connection != null) Close(connection, "Đã ngắt kết nối.");
    }

    public void Dispose()
    {
        _disposed = true;
        Disconnect();
    }
}

public sealed class ProtocolRequestException : Exception
{
    public string Code { get; }

    public ProtocolRequestException(string code, string message) : base(message)
    {
        Code = code;
    }
}
