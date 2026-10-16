using System.Text.Json;

namespace ChatApp.Shared.Protocol;

// Phần bao ngoài của một yêu cầu, phản hồi hoặc sự kiện.
public class Packet
{
    public int Version { get; set; } = 1;
    public string Type { get; set; } = string.Empty;

    // Phản hồi dùng lại ID của yêu cầu; sự kiện chủ động từ server dùng null.
    public string? RequestId { get; set; }
    public JsonElement? Payload { get; set; }
}
