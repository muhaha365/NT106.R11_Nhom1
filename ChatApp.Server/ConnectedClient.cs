using System.Net.Sockets;
using ChatApp.Shared;

namespace ChatApp.Server;
/// <summary>
/// Quản lý một kết nối TCP và danh tính đã xác thực của kết nối đó.
/// Khung lớp: chưa triển khai chức năng.
/// </summary>
public class ConnectedClient : IDisposable
{
    // TODO: Một vòng đọc; ghi tuần tự; xử lý client mất kết nối.
    // TODO: Đưa gói đã đọc tới RequestDispatcher; không tin senderId do client tự khai.
    public TcpClient TcpClient { get;}
    public int Id;
    public PacketIO packetIO;
    public int? UserId { get; set; }
    public DateTimeOffset? SessionExpiresAt  { get; set;}
    public ConnectedClient(TcpClient tcpClient, int id)
    {
        Id = id;
        TcpClient = tcpClient;
        packetIO = new PacketIO(tcpClient.GetStream());
    }

    public async Task<Packet?> ReceivePacketAsync(CancellationToken ct)
    {
        return await packetIO.ReceivePacketAsync(ct);
    }
    public async Task SendPacketAsync(Packet packet, CancellationToken ct)
    {
        await packetIO.SendPacketAsync(packet, ct);
    }
    public void Dispose()
    {
        TcpClient.Dispose();
    }
}
