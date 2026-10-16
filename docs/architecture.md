Solution có ba project, với hai chương trình chạy độc lập là Client và Server.

```text
Client A ── TCP ──┐
Client B ── TCP ──┼── Server ── Database
Client C ── TCP ──┘

Client ── ProjectReference ──> Shared
Server ── ProjectReference ──> Shared
```

Sơ đồ TCP là thiết kế đích. Khung hiện chưa có kết nối mạng hay database.

| Thư mục | Trách nhiệm |
|---|---|
| Client/Forms | LoginForm, RegisterForm, MainForm, CreateGroupForm |
| Client/Controls | MessageBubbleControl và FriendListControl là UserControl để gắn vào Form |
| Client/Networking | ChatConnection quản lý một kết nối TCP dùng chung |
| Client/Services | Tạo yêu cầu và xử lý kết quả cho Auth, Chat, Friends, Groups |
| Client/State | ClientSession giữ thông tin người dùng hiện tại trong bộ nhớ |
| Client/Resources | Icon và ảnh giao diện |
| Server/Networking | ChatServer nhận kết nối; ClientConnection quản lý từng kết nối; RequestDispatcher phân loại gói |
| Server/Services | Kiểm tra quyền và xử lý Auth, Chat, Friends, Groups, Presence |
| Server/Data | DatabaseConnectionFactory tạo kết nối theo cấu hình server |
| Server/Data/Repositories | UserRepository, MessageRepository, FriendRepository, GroupRepository đọc/ghi database |
| Server/Models | Model nội bộ theo schema database, chưa tự tạo bảng hay model |
| Server/Security | PasswordService tạo/kiểm tra giá trị băm mật khẩu |
| Shared/Configuration | Host, port, giới hạn gói mặc định |
| Shared/Contracts | Dữ liệu gửi/nhận, không chứa SQL hay thao tác giao diện |
| Shared/Protocol | Packet, tên loại gói, bộ chuyển đổi JSON cần triển khai |
| Shared/Transport | FrameCodec đọc/ghi gói trên Stream, cần triển khai |

Luồng gửi tin dự kiến:

```text
MainForm
  → Client.Services.ChatService
  → ChatConnection
  → TCP
  → Server.ClientConnection
  → RequestDispatcher
  → Server.Services.ChatService
  → MessageRepository
  → gửi sự kiện tới người nhận
```

Các quy tắc khi triển khai:

1. Chỉ server truy cập database và giữ thông tin kết nối database.
2. Form xử lý tương tác/hiển thị. Service xử lý chức năng. Repository xử lý truy vấn.
3. Một client dùng một ChatConnection. Không tạo kết nối riêng cho mỗi Form.
4. Mỗi kết nối có một vòng đọc và việc ghi được tuần tự hóa.
5. Dùng I/O bất đồng bộ; đưa cập nhật control về luồng UI qua Invoke/InvokeAsync.
6. Server lấy danh tính người gửi từ kết nối đã xác thực và kiểm tra quyền truy cập cuộc trò chuyện.
7. Thời gian tin nhắn do server tạo theo UTC; client đổi sang giờ địa phương khi hiển thị.
8. Khi đăng xuất/mất kết nối, xóa trạng thái client và xử lý trạng thái online ở server. Chốt chính sách nhiều phiên của một tài khoản.
9. Shared.UserSummaryDto không chứa password hash. Model xác thực nội bộ nằm ở server.
10. PasswordService cần dùng cơ chế băm mật khẩu chuyên dụng; không dùng SHA256 trực tiếp để lưu mật khẩu.
11. TLS qua SslStream là hướng bảo vệ đường truyền; không tự viết thuật toán mã hóa, không bỏ qua xác thực chứng chỉ trong bản triển khai. TLS không phải mã hóa đầu cuối.
12. Không log mật khẩu, token hoặc toàn bộ gói xác thực.

Khung chỉ dùng thư viện có sẵn của .NET. Chưa thêm package database hay password hashing, vì nhóm cần chốt công nghệ và schema trước.

Các phần mở rộng sau này:

| Tính năng | Nơi bổ sung |
|---|---|
| Typing/Seen | Contracts/Chat, ChatService hai phía, MessageRepository |
| Notification | Client/Services/NotificationService và sự kiện server |
| Tìm tin nhắn | ChatService, MessageRepository |
| File/ảnh/video | Contracts và service truyền tệp; metadata database; vùng lưu tệp riêng ở server |
| Emoji | Nội dung text Unicode và bộ chọn ở Client |
| Email/SĐT/Google | Contracts/Auth, AuthService, liên kết danh tính với tài khoản nội bộ |
| Lưu phiên | Cơ chế token và lưu an toàn; không lưu mật khẩu gốc |

Seen của nhóm cần trạng thái theo từng thành viên, không chỉ một IsSeen chung trên tin nhắn. Client không được tự quyết định quyền quản lý nhóm. Popup khi app đang chạy và thông báo lúc app đã đóng là hai phạm vi khác nhau.
