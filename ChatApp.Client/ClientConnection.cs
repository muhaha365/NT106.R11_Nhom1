using System.Net.Sockets;
namespace ChatApp.Client;

/// <summary>
/// Giữ một kết nối TCP dùng chung trong ứng dụng client.
/// Khung lớp: chưa triển khai chức năng.
/// </summary>
public class ClientConnection : IDisposable
{
    private TcpClient? _tcpClient;
    
    public async Task ConnectAsync(string host, int port)
    {
        Disconnect();
        TcpClient tcpClient = new TcpClient();
        _tcpClient = tcpClient;
        try
        {
            await tcpClient.ConnectAsync(host, port);
        }
        catch
        {
            Disconnect();
            throw;
        }
    }
    public void Disconnect()
    {
        _tcpClient?.Dispose();
        _tcpClient = null;
    }
    public void Dispose()
    {
        Disconnect();
    }
    // TODO: Kết nối/ngắt kết nối bất đồng bộ; một vòng nhận gói; tuần tự hóa việc ghi.
    // TODO: Phát sự kiện dữ liệu tới service. Không truy cập trực tiếp control WinForms.
}
