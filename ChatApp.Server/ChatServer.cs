using System.Net;
using System.Text.Json;
using ChatApp.Shared;
using System.Net.Sockets;
using ChatApp.Shared.Contracts;
using FirebaseAdmin.Auth;
namespace ChatApp.Server;

/// <summary>
/// Lắng nghe TCP và tiếp nhận nhiều kết nối client.
/// </summary>
public class ChatServer
{
    private readonly List<ConnectedClient> _clients = new List<ConnectedClient>();
    private readonly Database _database;
    public ChatServer(Database database)
    {
        _database = database;
    }
    private int _nextId = 0;
    private readonly object _sessionlock = new object();
    private readonly Dictionary<int, ConnectedClient> _onlineUsers = new();
    // Tra thông báo theo mã lỗi, dùng chung cho các nhánh xác thực.
    private static readonly Dictionary<string, string> AuthErrors = new()
    {
        ["PROFILE_REQUIRED"] = "Tài khoản đã có, cần hoàn tất hồ sơ ứng dụng.",
        ["USERNAME_TAKEN"] = "Tên người dùng đã được sử dụng.",
        ["ALREADY_ONLINE"] = "Tài khoản đang đăng nhập ở kết nối khác.",
        ["INVALID_TOKEN"] = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn.",
        ["USER_DISABLED"] = "Tài khoản đã bị vô hiệu hóa."
    };
    public async Task Run(int port, CancellationToken ct)
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, port);
        try
        {
            listener.Start();
            Console.WriteLine($"Server đang chờ tại 127.0.0.1: {port}");
            while (true)
            {
                TcpClient tcpClient = await listener.AcceptTcpClientAsync(ct);
                ConnectedClient client = new ConnectedClient(tcpClient, _nextId++);
                lock(_sessionlock)
                {
                    _clients.Add(client);
                }
                Console.WriteLine($"Client {tcpClient.Client.RemoteEndPoint} đã kết nối.");
                
                _ = HandleClientAsync(client, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Console.WriteLine("Server đã dừng.");
        }
        finally
        {
            listener.Stop();
            lock(_sessionlock)
            {
                foreach (ConnectedClient client in _clients)
                {
                    client.Dispose();
                }
            }
        }
    }
    private ConnectedClient[] GetConnectedClients()
    {
        lock(_sessionlock)
        {
            var connected = new List<ConnectedClient>();
            foreach (var client in _clients)
            {
                if (client.UserId == null || client.SessionExpiresAt == null
                    || client.SessionExpiresAt <= DateTimeOffset.UtcNow)
                {
                    continue;
                }
                connected.Add(client);
            }
            return connected.ToArray();
        }
    }
    private async Task HandleClientAsync(ConnectedClient sender, CancellationToken ct)
    {
        try
        {
            while(true)
            {
                Packet? packet = await sender.ReceivePacketAsync(ct);
                if (packet == null)
                {
                    Console.WriteLine($"Client {sender.Id} đã ngắt kết nối.");
                    break;
                }
                if(packet.Version != 1 || string.IsNullOrWhiteSpace(packet.Type))
                {
                    break;
                }
                bool isRequest = packet.Type.EndsWith(".request", StringComparison.Ordinal);
                if(isRequest && !Guid.TryParse(packet.RequestId, out _))
                {
                    break;
                }
                
                if(sender.UserId is not null && (sender.SessionExpiresAt is null || DateTimeOffset.UtcNow >= sender.SessionExpiresAt))
                {
                    if(isRequest)
                    {
                        await ReplyAsync(sender, packet, null, "NOT_AUTHENTICATED", "Phiên hết hạn. Hãy đăng nhập lại", ct);
                    }
                    break;
                }

                if(packet.Type == "auth.firebase.request")
                {
                    await HandleFirebaseLoginAsync(sender, packet, ct);
                    continue;
                }
                
                if(sender.UserId is null)
                {
                    if(isRequest)
                    {
                        await ReplyAsync(sender, packet, null, "NOT_AUTHENTICATED", "Bạn cần đăng nhập.", ct);
                    }
                    break;
                }
                if(packet.Type == "auth.logout.request")
                {
                    ReleaseSession(sender);
                    await ReplyAsync(sender, packet, new EmptyResult(), null, null, ct);
                    break;
                }

                if(isRequest)
                {
                    await sender.SendPacketAsync(new Packet
                    {
                        Type = "system.error.response",
                        RequestId = packet.RequestId,
                        Payload = JsonSerializer.SerializeToElement(new
                        {
                            Code = "NOT_IMPLEMENTED",
                            Message = "Chức năng này chưa phát triển"
                        })
                    }, ct);
                    continue;
                }
                if (packet.Type != "chat")
                {
                    Console.WriteLine($"Client {sender.Id} gửi gói không hợp lệ: {packet.Type}");
                    continue;
                }
                if (!packet.Payload.HasValue || packet.Payload.Value.ValueKind != JsonValueKind.String)
                {
                    Console.WriteLine($"Client {sender.Id} gửi gói chat không hợp lệ.");
                    continue;
                }
                string? message = packet.Payload.Value.GetString();
                if(string.IsNullOrWhiteSpace(message))
                {
                    Console.WriteLine($"Client {sender.Id} gửi gói chat rỗng.");
                    continue;
                }
                string reply = $"Client {sender.Id} gửi: {message}";
                Console.WriteLine($"Client {sender.Id} gửi: {message}");
                Packet send = new Packet
                {
                    Type = "chat", 
                    Payload = JsonSerializer.SerializeToElement(reply)
                };

                foreach (var receiver in GetConnectedClients())
                {
                    if (receiver.Id == sender.Id)
                    {
                        continue;
                    }

                    try
                    {
                        await receiver.SendPacketAsync(send, ct);
                    }
                    catch (Exception ex)
                    {
                        if(!ct.IsCancellationRequested)
                        {
                            Console.WriteLine($"Không gửi được tới Client {receiver.Id}: {ex.Message}");
                        }
                        receiver.Dispose();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if(!ct.IsCancellationRequested)
            {
                Console.WriteLine($"Lỗi khi xử lý Client {sender.Id}: {ex.Message}");
            }
        }
        finally
        {
            lock (_sessionlock)
            {
                _clients.Remove(sender);
            }
            ReleaseSession(sender);
            sender.Dispose();
        }
    }

    private static async Task ReplyAsync(ConnectedClient client, Packet request, object? data, string? code, string? message, CancellationToken ct = default)
    {
        string requestName = request.Type.Substring(0, request.Type.Length - ".request".Length);
        var payload = new
        {
            Success = code is null,
            Data = code is null ? data : null,
            Error = code is null ? null : new {Code = code, Message = message}
        };
        await client.SendPacketAsync(new Packet
        {
            Type = requestName + ".response",
            RequestId = request.RequestId,
            Payload = JsonSerializer.SerializeToElement(payload)
        }, ct);
    }
    private void ReleaseSession(ConnectedClient client)
    {
        lock(_sessionlock)
        {
            if (client.UserId.HasValue)
            {
                int id = client.UserId.Value;
                if (_onlineUsers.TryGetValue(id, out var owner) && ReferenceEquals(client, owner))
                {
                    _onlineUsers.Remove(id);
                }
            }
            client.UserId = null;
            client.SessionExpiresAt = null;
        }
    }
    private async Task HandleFirebaseLoginAsync(ConnectedClient client, Packet packet, CancellationToken ct = default)
    {
        if (client.UserId != null)
        {
            await ReplyAsync(client, packet, null, "ALREADY_AUTHENTICATED", "Kết nối này đã đăng nhập.", ct);
            return;
        }
        if (!packet.Payload.HasValue || packet.Payload.Value.ValueKind != JsonValueKind.Object)
        {
            await ReplyAsync(client, packet, null, "INVALID_REQUEST", "Thông tin gửi lên không hợp lệ. Hãy kiểm tra lại.", ct);
            return;
        }

        AuthenticationResult? result = null;
        string? code = null;
        string? message = null;
        try
        {
            JsonElement payload = packet.Payload.Value;
            FirebaseLoginRequest? request = JsonSerializer.Deserialize<FirebaseLoginRequest>(payload);
            if(request is null || string.IsNullOrWhiteSpace(request.IdToken) || request.IdToken.Length > 16384)
            {
                throw new ArgumentException("INVALID_REQUEST");
            }
            var verified = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken, checkRevoked: true, cancellationToken: ct);
            var account = await FirebaseAuth.DefaultInstance.GetUserAsync(verified.Uid, ct);
            if(account.Disabled)
            {
                throw new InvalidOperationException("USER_DISABLED");
            }
            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(verified.ExpirationTimeSeconds);
            var user = await _database.GetUserByFirebaseUidAsync(verified.Uid, ct);
            if(user is null)
            {
                if(string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.DisplayName))
                {
                    throw new InvalidOperationException("PROFILE_REQUIRED");
                }
                user = await _database.EnsureProfileAsync(verified.Uid, request.Username, request.DisplayName, ct);
            }
            lock(_sessionlock)
            {
                if(DateTimeOffset.UtcNow >= expiresAt)
                {
                    throw new InvalidOperationException("INVALID_TOKEN");
                }
                if(_onlineUsers.ContainsKey(user.Id))
                {
                    throw new InvalidOperationException("ALREADY_ONLINE");
                }
                client.UserId = user.Id;
                client.SessionExpiresAt = expiresAt;
                _onlineUsers.Add(user.Id, client);
            }
            UserSummary onlineUser = new UserSummary(user.Id, user.Username, user.DisplayName, true);
            result = new AuthenticationResult(onlineUser);
        }
        catch (OperationCanceledException) when(ct.IsCancellationRequested)
        {
            throw;
        }
        catch (FirebaseAuthException ex)
        {
            bool rejected = ex.AuthErrorCode == AuthErrorCode.InvalidIdToken
                || ex.AuthErrorCode == AuthErrorCode.ExpiredIdToken
                || ex.AuthErrorCode == AuthErrorCode.RevokedIdToken
                || ex.AuthErrorCode == AuthErrorCode.UserNotFound;
            if (rejected)
            {
                code = "INVALID_TOKEN";
                message = "Phiên Firebase không hợp lệ. Hãy đăng nhập lại.";
            }
            else
            {
                code = "AUTH_UNAVAILABLE";
                message = "Chưa thể xác minh tài khoản. Hãy thử lại sau.";
            }
        }
        catch (JsonException)
        {
            code = "INVALID_REQUEST";
            message = "Payload không hợp lệ";
        }
        catch (InvalidOperationException ex) when (AuthErrors.ContainsKey(ex.Message))
        {
            code = ex.Message;
            message = AuthErrors[code];
        }
        catch (ArgumentException ex) when (ex.Message == "INVALID_REQUEST")
        {
            code = "INVALID_REQUEST";
            message = "Thông tin gửi lên không hợp lệ. Hãy kiểm tra lại.";
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine($"Auth thất bại: {ex.GetType().Name}");
            code = "INTERNAL_ERROR";
            message = "Server chưa hoàn tất xác thực. Hãy thử lại sau:";
        }
        await ReplyAsync(client, packet, result, code, message, ct);
    }
}
