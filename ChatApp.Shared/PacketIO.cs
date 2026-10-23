using System.Buffers.Binary;
using System.Text.Json;

namespace ChatApp.Shared;

public class PacketIO
{
    private readonly StreamReader _sr;
    private readonly StreamWriter _sw;
    private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

    public PacketIO(Stream stream)
    {
        _sr = new StreamReader(stream);
        _sw = new StreamWriter(stream);
        _sw.AutoFlush = true;
    }
    public async Task SendPacketAsync(Packet packet)
    {
        string json = JsonSerializer.Serialize(packet);
        await _sendLock.WaitAsync();
        try
        {
            await _sw.WriteLineAsync(json);
        }
        finally
        {
            _sendLock.Release();
        }
    }
    public async Task<Packet?> ReceivePacketAsync()
    {
        string? json = await _sr.ReadLineAsync();
        if (json == null)
        {
            return null;
        }
        Packet? packet = JsonSerializer.Deserialize<Packet>(json);
        if(packet == null)
        {
            throw new InvalidDataException("Dữ liệu nhận được không hợp lệ.");
        }
        return packet;
    }
}