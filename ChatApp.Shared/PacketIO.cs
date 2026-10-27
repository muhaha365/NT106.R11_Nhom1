using System.Buffers.Binary;
using System.Text.Json;
using System.Text;

namespace ChatApp.Shared;

public class PacketIO
{
    private readonly Stream _stream;
    private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
    private const int MaxPacketSize = 1024 * 1024;

    public PacketIO(Stream stream)
    {
        _stream = stream;
    }
    public async Task SendPacketAsync(Packet packet, CancellationToken ct = default)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(packet);
        if(payload.Length > MaxPacketSize)
        {
            throw new InvalidDataException($"Kích thước gói tin vượt quá giới hạn {MaxPacketSize} bytes.");
        }
        byte[] header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, payload.Length);
        await _sendLock.WaitAsync(ct);
        try
        {
            await _stream.WriteAsync(header, ct);
            await _stream.WriteAsync(payload, ct);
        }
        catch(OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Lỗi khi gửi gói tin: {ex.Message}");
            throw;
        }
        finally
        {
            _sendLock.Release();
        }
    }
    public async Task<Packet?> ReceivePacketAsync(CancellationToken ct = default)
    {
        byte[] header = new byte[4];
        int headerBytes = await ReadByte(header, ct);
        if(headerBytes == 0)
        {
            return null;
        }
        if(headerBytes < header.Length)
        {
            throw new InvalidDataException("Không đủ dữ liệu để đọc header.");
        }
        int payloadLength = BinaryPrimitives.ReadInt32BigEndian(header);
        if(payloadLength < 0 || payloadLength > MaxPacketSize)
        {
            throw new InvalidDataException($"Kích thước gói tin không hợp lệ: {payloadLength} bytes.");
        }
        byte[] payload = new byte[payloadLength];
        int payloadBytes = await ReadByte(payload, ct);
        if(payloadBytes < payloadLength)
        {
            throw new InvalidDataException("Không đủ dữ liệu để đọc payload.");
        }
        Packet? packet = JsonSerializer.Deserialize<Packet>(payload);
        if(packet == null)
        {
            throw new InvalidDataException("Gói tin không hợp lệ");
        }
        return packet;
    }
    private async Task<int> ReadByte(byte[] buffer, CancellationToken ct)
    {
        int totalBytesRead = 0;
        while (totalBytesRead < buffer.Length)
        {
            int bytesRead = await _stream.ReadAsync(buffer, totalBytesRead, buffer.Length - totalBytesRead, ct);
            if(bytesRead == 0)
            {
                break;
            }
            totalBytesRead += bytesRead;
        }
        return totalBytesRead;
    }
}