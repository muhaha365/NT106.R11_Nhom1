ChatApp.Nhom1 — khung đồ án nhắn tin WinForms, C# / .NET 10.

Mở `ChatApp.Nhom1.slnx` bằng Visual Studio 2026 có workload .NET desktop development. Solution gồm ba project:

| Project | Framework | Vai trò |
|---|---|---|
| ChatApp.Client | net10.0-windows | Giao diện WinForms, sau này kết nối tới server |
| ChatApp.Server | net10.0 | Console server, sau này xử lý TCP và database |
| ChatApp.Shared | net10.0 | DTO, cấu hình mặc định và quy ước giao tiếp dùng chung |

Client và Server đều tham chiếu Shared. Shared không tham chiếu ngược lại.

Tình trạng hiện tại:

- Solution chứa đủ ba project và có thể build cùng nhau.
- Client mở LoginForm. Bốn form và hai UserControl vẫn là giao diện trống để nhóm thiết kế tiếp.
- Server in thông tin khung và chờ Enter. Chưa lắng nghe cổng TCP.
- Đã có DTO đăng nhập/đăng ký, trạng thái client trong bộ nhớ, cấu trúc Packet và tên một số loại gói.
- Các class Networking, Services, Repositories, PasswordService, PacketSerializer và FrameCodec mới chứa mô tả/TODO.
- Chưa có xác thực, chat, database, TLS, truyền tệp hay lưu phiên hoạt động. Không có nút đăng nhập giả hoặc kết quả nghiệp vụ giả.

Chạy bằng Visual Studio:

1. Mở solution. Nếu solution đang mở từ trước, chấp nhận Reload khi Visual Studio thông báo file thay đổi trên ổ đĩa.
2. Chọn Build → Build Solution (Ctrl + Shift + B).
3. Chuột phải ChatApp.Client → Set as Startup Project → F5 để mở LoginForm.
4. Dừng chạy. Đặt ChatApp.Server làm startup project → F5 để kiểm tra Console.
5. Shared là thư viện, không đặt làm startup project.

Chạy từ Terminal tại thư mục solution:

```powershell
dotnet build .\ChatApp.Nhom1.slnx
dotnet run --project .\ChatApp.Server\ChatApp.Server.csproj
dotnet run --project .\ChatApp.Client\ChatApp.Client.csproj
```

Lệnh chạy Server chờ Enter. Dùng Terminal khác nếu muốn chạy Client cùng lúc. Việc hai chương trình mở được hiện chỉ kiểm tra khung, chưa kiểm tra kết nối.

Đọc tiếp:

- [Hướng dẫn tiếp tục trong Visual Studio](docs/getting-started.md)
- [Cấu trúc và nhiệm vụ các class](docs/architecture.md)
- [Đặc tả protocol JSON và các gói mẫu](docs/protocol.md)
- [Các tình huống kiểm thử](docs/test-cases.md)
- [Tích hợp database](database/README.md)

Cấu hình mặc định nằm ở `ChatApp.Shared/Configuration/NetworkDefaults.cs`: host 127.0.0.1, port 5000. Khi triển khai LAN, client cần IP của máy server; server cần địa chỉ lắng nghe phù hợp. Cấu hình này chưa mở cổng và chưa thiết lập firewall.

Trước khi làm theo timeline, nhóm cần đối chiếu phạm vi với giảng viên: username/password chưa bao phủ Email/SĐT/Google; file/ảnh/video và thông báo khi ứng dụng đóng cũng chưa được thực hiện.

Không commit .vs, bin, obj, mật khẩu, token hoặc tệp người dùng tải lên. Giữ các file .Designer.cs và .resx trong Git. .gitignore đã có sẵn; chưa khởi tạo hoặc liên kết remote Git.
