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
    public async Task RunAsync(int port, CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        var clients = new List<ConnectedClient>();
        try
        {
            listener.Start();
            Console.WriteLine($"Server đang lắng nghe tại 127.0.0.1:{port}");
            while(!cancellationToken.IsCancellationRequested)
            {
                TcpClient tcpClient = await listener.AcceptTcpClientAsync(cancellationToken);
                var client = new ConnectedClient(tcpClient);
                clients.Add(client);
                Console.WriteLine($"Đã nhận kết nối từ: {tcpClient.Client.RemoteEndPoint}");
            }
        }
        catch(OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {

        }
        finally
        {
            listener.Stop();
            foreach (ConnectedClient client in clients)
            {
                client.Dispose();
            }
            Console.WriteLine("Server đã dừng");
        }
    }
    // TODO: Dùng TcpListener; tạo ClientConnection cho từng kết nối.
    // TODO: Hỗ trợ hủy tác vụ và đóng các kết nối khi server dừng.
}
