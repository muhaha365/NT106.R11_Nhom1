using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
namespace ChatApp.Server;

/// <summary>
/// Lắng nghe TCP và tiếp nhận nhiều kết nối client.
/// Khung lớp: chưa triển khai chức năng.
/// </summary>
public class ChatServer
{
    public async Task Run(int port)
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, port);
        List<ConnectedClient> clients = new List<ConnectedClient>();
        try
        {
            listener.Start();
            Console.WriteLine($"Server đang chờ tại 127.0.0.1: {port}");
            while (true)
            {
                TcpClient tcpClient = await listener.AcceptTcpClientAsync();
                ConnectedClient client = new ConnectedClient(tcpClient);
                clients.Add(client);
                Console.WriteLine($"Client {tcpClient.Client.RemoteEndPoint} đã kết nối.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi: {ex.Message}");
        }
        finally
        {
            listener.Stop();
            foreach (ConnectedClient client in clients)
            {
                client.Dispose();
            }
        }
    }
}
