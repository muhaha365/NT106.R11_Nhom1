using System.Net.Sockets;
using ChatApp.Shared;
namespace ChatApp.Client;

/// <summary>
/// Giữ một kết nối TCP dùng chung trong ứng dụng client.
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

    public async Task<Packet?> ReceiveAsync()
    {
        if(_tcpClient == null)
        {
            throw new InvalidOperationException("Chưa kết nối tới server.");
        }
        return await _packetIO!.ReceivePacketAsync();
    }
    public async Task SendAsync(Packet packet)
    {
        if (_tcpClient == null)
        {
            return;
        }
        await _packetIO!.SendPacketAsync(packet);
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
}
