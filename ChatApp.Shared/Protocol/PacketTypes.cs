namespace ChatApp.Shared.Protocol;

// Các tên khởi đầu để hai phía không tự gõ những chuỗi khác nhau.
// Có tên gói không có nghĩa là chức năng tương ứng đã được triển khai.
public static class PacketTypes
{
    public const string PingRequest = "system.ping.request";
    public const string PingResponse = "system.ping.response";
    public const string LoginRequest = "auth.login.request";
    public const string LoginResponse = "auth.login.response";
    public const string RegisterRequest = "auth.register.request";
    public const string RegisterResponse = "auth.register.response";
    public const string ErrorResponse = "system.error.response";
}
