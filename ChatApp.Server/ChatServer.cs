using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using ChatApp.Shared;
using System.Net.Sockets;
using System.Diagnostics;
using ChatApp.Shared.Contracts;
using FirebaseAdmin.Auth;
using System.ComponentModel;
namespace ChatApp.Server;

/// <summary>
/// Lắng nghe TCP và tiếp nhận nhiều kết nối client.
/// Khung lớp: chưa triển khai chức năng.
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
                _clients.Add(client);
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
            foreach (ConnectedClient client in _clients)
            {
                client.Dispose();
            }
        }
    }
    private ConnectedClient[] GetConnectedClients()
    {
        return _clients.ToArray();
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
                if (packet.Type != "chat")
                {
                    Console.WriteLine($"Client {sender.Id} gửi gói không hợp lệ: {packet.Type}");
                    continue;
                }
                if (packet.Payload is not JsonElement payload || payload.ValueKind != JsonValueKind.String)
                {
                    Console.WriteLine($"Client {sender.Id} gửi gói chat không hợp lệ.");
                    continue;
                }
                string message = payload.GetString()!;
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
            _clients.Remove(sender);
            sender.Dispose();
        }
    }

    private static async Task ReplyAsync(ConnectedClient client, Packet request, object? data, string? code, string? message, CancellationToken ct = default)
    {
        var payload = new
        {
            Success = code is null,
            Data = code is null ? data : null,
            Error = code is null ? null : new {Code = code, Message = message}
        };
        await client.SendPacketAsync(new Packet
        {
            Type = $"{request.Type[..^8]}.response",
            RequestId = request.RequestId,
            Payload = JsonSerializer.SerializeToElement(payload)
        }, ct);
    }
    private void ReleaseSession(ConnectedClient client)
    {
        lock(_sessionlock)
        {
            if(client.UserId is int id && _onlineUsers.TryGetValue(id, out var owner) && ReferenceEquals(client, owner))
            {
                _onlineUsers.Remove(id);
            }
            client.UserId = null;
            client.SessionExpiresAt = null;
        }
    }
    private async Task HandleFirebaseLoginAsync(ConnectedClient client, Packet packet, CancellationToken ct = default)
    {
        AuthenticationResult? result = null;
        string? code = null;
        string? message = null;
        try
        {
            if(client.UserId is not null)
            {
                throw new InvalidOperationException("ALREADY_AUTHENTICATED");
            }
            if(packet.Payload is not JsonElement payload || payload.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("INVALID_REQUEST");
            }
            var request = JsonSerializer.Deserialize<FirebaseLoginRequest>(payload);
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
                    throw new InvalidDataException("PROFILE_REQUIRED");
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
                _onlineUsers.Add(user.Id, client);
            }
            result = new AuthenticationResult(user with { IsOnline = true });
        }
        catch (OperationCanceledException) when(ct.IsCancellationRequested) { throw; }
        catch (FirebaseAuthException ex)
        {
            bool rejected = ex.AuthErrorCode is AuthErrorCode.InvalidIdToken
                or AuthErrorCode.ExpiredIdToken or AuthErrorCode.RevokedIdToken
                or AuthErrorCode.UserNotFound;
            code = rejected ? "INVALID_TOKEN" : "AUTH_UNAVAILABLE";
            message = rejected ? "Phiên Firebase không hợp lệ. Hãy đăng nhập lại." : "Chưa thể xác minh tài khoản. Hãy thử lại sau.";
        }
        catch (JsonException) { code = "INVALID_REQUEST"; message = "Payload không hợp lệ";}
        catch (InvalidOperationException ex) when (ex.Message is "PROFILE_REQUIRED"
            or "USERNAME_TAKEN" or "ALREADY_AUTHENTICATED" or "ALREADY_ONLINE" or "INVALID_TOKEN"
            or "USER_DISABLED")
        {
            code = ex.Message;
            message = code switch
            {
                "PROFILE_REQUIRED" => "Tài khoản đã có, cần hoàn tất hồ sơ ứng dụng.",
                "USERNAME_TAKEN" => "Tên người dùng đã được sử dụng.",
                "ALREADY_ONLINE" => "Tài khoản đang đăng nhập ở kết nối khác.",
                "INVALID_TOKEN" => "Tài khoản đã bị vô hiệu hóa.",
                _ => "Kết nối này dã đăng nhập."
            };
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine($"Auth thất bại: {ex.GetType().Name}");
            code = "INTERNAL_ERROR";
            message = "Server chưa hoàn tất xác thực. HÃy thử lại sau:";
        }
        await ReplyAsync(client, packet, result, code, message, ct);
    }
}
