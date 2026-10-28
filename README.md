ChatApp.Nhom1 — khung đồ án nhắn tin WinForms, C# / .NET 10.

Mở `ChatApp.Nhom1.slnx` bằng Visual Studio 2026 có workload .NET desktop development. Solution gồm ba project:

| Project | Framework | Vai trò |
|---|---|---|
| ChatApp.Client | net10.0-windows | WinForms, UI bạn bè/nhóm và client giao tiếp theo protocol |
| ChatApp.Server | net10.0 | Console server TCP, hiện hỗ trợ chat broadcast demo |
| ChatApp.Shared | net10.0 | DTO, Packet và quy ước giao tiếp dùng chung |

Client và Server đều tham chiếu Shared. Shared không tham chiếu ngược lại.

Tình trạng hiện tại:

- Cả ba project có thể build cùng solution.
- Client mở LoginForm. Chọn **Demo chat** để mở MainForm có UI bạn bè, lời mời, tìm người, nhóm và thành viên.
- ProtocolClient quản lý một vòng nhận chung, ghép response theo RequestId, phát event và xử lý timeout/mất kết nối.
- Server lắng nghe 127.0.0.1:5000, chuyển tiếp gói `chat` tới các client khác. Tab **Chat chung (demo)** giữ tương thích với luồng này.
- UI bạn bè/nhóm đã nối request/response/event theo protocol; các nút nghiệp vụ chỉ mở khi nhận kết nối và UserSummary từ login thật.
- Xác thực, nghiệp vụ bạn bè/nhóm, database, TLS và lưu phiên phía server chưa được triển khai. Giao diện không tự tạo tài khoản hoặc dữ liệu người dùng giả.

Chạy bằng Visual Studio:

1. Mở solution. Nếu solution đang mở từ trước, chấp nhận Reload khi Visual Studio thông báo file thay đổi trên ổ đĩa.
2. Chọn Build → Build Solution (Ctrl + Shift + B).
3. Chuột phải ChatApp.Client → Set as Startup Project → F5 để mở LoginForm.
4. Chạy ChatApp.Server trong một Terminal riêng; mở Client, chọn Demo chat và Kết nối để thử tab Chat chung (demo).
5. Shared là thư viện, không đặt làm startup project.

Chạy từ Terminal tại thư mục solution:

```powershell
dotnet build .\ChatApp.Nhom1.slnx
dotnet run --project .\ChatApp.Server\ChatApp.Server.csproj
dotnet run --project .\ChatApp.Client\ChatApp.Client.csproj
```

Server dùng Ctrl+C để dừng. Có thể mở nhiều Client để thử gửi/nhận chat chung. Bạn bè và chat nhóm cần handler/auth phía server trước khi sử dụng với dữ liệu thật.

Đọc tiếp:

- [Cách mở UI bạn bè và nhóm, điểm ghép login/server](docs/friends-and-groups.md)
- [Đặc tả protocol JSON và các gói mẫu](docs/protocol.md)
- [Tích hợp database](database/README.md)

MainForm cho nhập máy chủ/cổng, mặc định 127.0.0.1:5000. Server hiện chỉ lắng nghe loopback trên cùng máy. Khi triển khai LAN cần đổi địa chỉ lắng nghe phù hợp và cấu hình firewall.

Trước khi làm theo timeline, nhóm cần đối chiếu phạm vi với giảng viên: username/password chưa bao phủ Email/SĐT/Google; file/ảnh/video và thông báo khi ứng dụng đóng cũng chưa được thực hiện.

Không commit .vs, bin, obj, mật khẩu, token, ảnh kiểm tra hoặc tệp người dùng tải lên. Giữ các file .Designer.cs và .resx trong Git.
