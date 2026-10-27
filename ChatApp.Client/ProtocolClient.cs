using System.Collections.Concurrent;
using System.Text.Json;
using ChatApp.Shared;

namespace ChatApp.Client;

/// <summary>Một vòng nhận chung, ghép response theo RequestId và tách event cho UI.</summary>
public sealed class ProtocolClient : IDisposable
{
    private sealed record Pending(ClientConnection Connection, string ResponseType, TaskCompletionSource<Packet> Completion);
    private readonly ConcurrentDictionary<string, Pending> _pending = new();
    private readonly SemaphoreSlim _connectGate = new(1, 1);
    private readonly TimeSpan _requestTimeout;
    private readonly JsonSerializerOptions _json = new()
    {
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true
    };
    private ClientConnection? _connection;
    private bool _disposed;

    public bool IsConnected => _connection is not null;
    public event Action<Packet>? EventReceived;
    public event Action<string>? Disconnected;

    public ProtocolClient(TimeSpan? requestTimeout = null)
    {
        _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(10);
        if (_requestTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(requestTimeout));
    }

    public async Task ConnectAsync(string host, int port)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _connectGate.WaitAsync().ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            Disconnect();
            var connection = new ClientConnection();
            try
            {
                await connection.ConnectAsync(host, port).WaitAsync(_requestTimeout).ConfigureAwait(false);
                ObjectDisposedException.ThrowIf(_disposed, this);
                _connection = connection;
                _ = ReceiveLoopAsync(connection);
            }
            catch { connection.Dispose(); throw; }
        }
        finally { _connectGate.Release(); }
    }

    public async Task<T> RequestAsync<T>(string requestType, object payload, CancellationToken cancellationToken = default) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!requestType.EndsWith(".request", StringComparison.Ordinal)) throw new ArgumentException("Loại gói phải là request.", nameof(requestType));
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();
        var connection = _connection ?? throw new InvalidOperationException("Chưa kết nối tới server.");
        var id = Guid.NewGuid().ToString();
        var pending = new Pending(connection, requestType[..^8] + ".response", new(TaskCreationOptions.RunContinuationsAsynchronously));
        _pending[id] = pending;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_requestTimeout);
        try
        {
            if (!ReferenceEquals(_connection, connection)) throw new IOException("Kết nối đã đóng trước khi gửi yêu cầu.");
            await connection.SendAsync(new Packet { Type = requestType, RequestId = id, Payload = JsonSerializer.SerializeToElement(payload) })
                .WaitAsync(timeout.Token).ConfigureAwait(false);
            var response = await pending.Completion.Task.WaitAsync(timeout.Token).ConfigureAwait(false);
            if (response.Payload is not JsonElement body || body.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Phản hồi của server không đúng định dạng.");
            if (response.Type == "system.error.response") throw ReadError(body);
            if (!body.TryGetProperty("Success", out var success) || success.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new InvalidDataException("Phản hồi thiếu trạng thái xử lý.");
            if (!success.GetBoolean())
            {
                if (!body.TryGetProperty("Data", out var failedData) || failedData.ValueKind != JsonValueKind.Null)
                    throw new InvalidDataException("Phản hồi lỗi không đúng định dạng.");
                var failure = ReadError(body.GetProperty("Error"));
                if (failure.Code == "NOT_AUTHENTICATED") Close(connection, "Phiên đăng nhập không còn hợp lệ. Hãy đăng nhập lại.");
                throw failure;
            }
            if (!body.TryGetProperty("Error", out var error) || error.ValueKind != JsonValueKind.Null ||
                !body.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Phản hồi thành công không đúng định dạng.");
            return data.Deserialize<T>(_json) ?? throw new InvalidDataException("Phản hồi không có dữ liệu.");
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

    public Task SendLegacyChatAsync(string content)
    {
        var connection = _connection ?? throw new InvalidOperationException("Chưa kết nối tới server.");
        return connection.SendAsync(new Packet { Type = "chat", Payload = JsonSerializer.SerializeToElement(content) });
    }

    private async Task ReceiveLoopAsync(ClientConnection connection)
    {
        string reason = "Server đã ngắt kết nối.";
        try
        {
            while (ReferenceEquals(_connection, connection))
            {
                var packet = await connection.ReceiveAsync().ConfigureAwait(false);
                if (packet is null) break;
                if (!ReferenceEquals(_connection, connection)) break;
                if (packet.Version != 1 || string.IsNullOrWhiteSpace(packet.Type)) throw new InvalidDataException();
                if (packet.RequestId is { } id)
                {
                    if (packet.Type.EndsWith(".event", StringComparison.Ordinal)) throw new InvalidDataException();
                    if (_pending.TryGetValue(id, out var pending) && ReferenceEquals(pending.Connection, connection))
                    {
                        if (packet.Type != pending.ResponseType && packet.Type != "system.error.response") throw new InvalidDataException();
                        pending.Completion.TrySetResult(packet);
                    }
                    // Phản hồi đến sau timeout/hủy không được ghép với yêu cầu mới.
                }
                else
                {
                    if (packet.Type.EndsWith(".response", StringComparison.Ordinal)) throw new InvalidDataException();
                    EventReceived?.Invoke(packet);
                }
            }
        }
        catch (Exception error) when (error is IOException or System.Net.Sockets.SocketException or ObjectDisposedException or JsonException or InvalidDataException)
        {
            reason = "Kết nối tới server đã đóng hoặc dữ liệu nhận không hợp lệ.";
        }
        finally { Close(connection, reason); }
    }

    private static ProtocolRequestException ReadError(JsonElement error)
    {
        if (error.ValueKind != JsonValueKind.Object || !error.TryGetProperty("Code", out var code) ||
            code.ValueKind != JsonValueKind.String || !error.TryGetProperty("Message", out var message) || message.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("Server trả lỗi không đúng định dạng.");
        return new ProtocolRequestException(code.GetString()!, message.GetString()!);
    }

    private void Close(ClientConnection connection, string reason)
    {
        if (!ReferenceEquals(Interlocked.CompareExchange(ref _connection, null, connection), connection)) return;
        connection.Dispose();
        foreach (var entry in _pending)
            if (ReferenceEquals(entry.Value.Connection, connection) && _pending.TryRemove(entry.Key, out var pending))
                pending.Completion.TrySetException(new IOException(reason));
        if (_connection is null) Disconnected?.Invoke(reason);
    }

    public void Disconnect()
    {
        if (_connection is { } connection) Close(connection, "Đã ngắt kết nối.");
    }

    public void Dispose() { _disposed = true; Disconnect(); }
}

public sealed class ProtocolRequestException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
