namespace ChatApp.Shared;

/// <summary>
/// Đọc và ghi một gói hoàn chỉnh trên Stream.
/// Khung lớp: chưa triển khai chức năng.
/// </summary>
public class PacketIO
{
    // TODO: 4 byte độ dài big-endian, theo sau là JSON UTF-8; đọc đủ từng phần.
    // TODO: Kiểm tra kích thước trước khi cấp phát; ngắt kết nối giữa gói phải báo lỗi.
    // TODO: Làm việc với Stream để có thể dùng NetworkStream hoặc SslStream về sau.
}
