using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using ChatApp.Shared;
using System.Net.Sockets;
namespace ChatApp.Server;

/// <summary>
/// Lắng nghe TCP và tiếp nhận nhiều kết nối client.
/// Khung lớp: chưa triển khai chức năng.
/// </summary>
public class ChatServer
{
    private readonly List<ConnectedClient> _clients = new List<ConnectedClient>();
    private int _nextId = 0;
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
                        await receiver.SendPacketAsync(send);
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
}
