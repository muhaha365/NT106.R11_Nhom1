using System.Text;
using ChatApp.Shared.Configuration;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("ChatApp.Server - Khung dự án Nhóm 1");
Console.WriteLine($"Cấu hình dự kiến: {NetworkDefaults.Host}:{NetworkDefaults.Port}");
Console.WriteLine("Chưa mở cổng TCP. Bước tiếp theo: triển khai Networking/ChatServer.cs.");
Console.WriteLine("Nhấn Enter để thoát.");
Console.ReadLine();
