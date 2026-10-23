using System.Buffers;
using System.Net.Sockets;
using System.Text;
using ChatApp.Shared;
namespace ChatApp.Client;

/// <summary>
/// Giữ một kết nối TCP dùng chung trong ứng dụng client.
/// Khung lớp: chưa triển khai chức năng.
/// </summary>
public class ClientConnection : IDisposable
{
    private TcpClient? _tcpClient;
    private PacketIO? _packetIO;
    
    public async Task ConnectAsync(string host, int port)
    {
        Disconnect();
        _tcpClient = new TcpClient();
        try
        {
            await _tcpClient.ConnectAsync(host, port);
            _packetIO = new PacketIO(_tcpClient.GetStream());
        }
        catch
        {
            Disconnect();
            throw;
        }
    }

    public async Task ReceiveLoopAsync()
    {
        if(_tcpClient == null)
        {
            throw new InvalidOperationException("Chưa kết nối tới server.");
        }

        try
        {
            while(true)
            {
                Packet? packet = await _packetIO.ReceivePacketAsync();
                if(packet == null)
                {
                    break;
                }
                if(packet.Type == "chat")
                {
                    string? message = packet.Payload?.GetString();
                    MessageBox.Show($"Nhận từ server: {message}");
                }
            }
        }
        finally
        {
            Disconnect();
        }
    }
    public async Task SendAsync(Packet packet)
    {
        if (_tcpClient == null)
        {
            return;
        }
        await _packetIO.SendPacketAsync(packet);
    }
    public void Disconnect()
    {
        _tcpClient?.Dispose();
        _tcpClient = null;
        _packetIO = null;
    }
    public void Dispose()
    {
        Disconnect();
    }
    // TODO: Kết nối/ngắt kết nối bất đồng bộ; một vòng nhận gói; tuần tự hóa việc ghi.
    // TODO: Phát sự kiện dữ liệu tới service. Không truy cập trực tiếp control WinForms.
}
