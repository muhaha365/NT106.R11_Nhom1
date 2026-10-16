namespace ChatApp.Shared.Configuration;

public static class NetworkDefaults
{
    // Địa chỉ dùng khi client và server chạy trên cùng máy.
    // Khi chạy qua LAN, client cần dùng IP của máy chạy server.
    public const string Host = "127.0.0.1";
    public const int Port = 5000;

    // Giới hạn phần JSON của mỗi gói: 1 MiB. Chưa có bộ đọc gói thực thi giới hạn này.
    public const int MaxPacketBytes = 1024 * 1024;
}
