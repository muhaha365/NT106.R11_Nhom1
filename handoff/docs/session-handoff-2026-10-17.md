Bản bàn giao session ChatApp.Nhom1 — ngày 17/10/2026

Tài liệu này tóm tắt yêu cầu, quyết định, thay đổi đã thực hiện và trạng thái kiểm tra để tiếp tục ở session mới. Những phần ghi là đề xuất/TODO chưa phải chức năng đã hoạt động. Trạng thái file và Git được đối chiếu lại khi viết bàn giao; kết quả build/runtime bên dưới là kết quả của lượt triển khai trước trong cùng session, không phải lần kiểm thử mới khi viết tài liệu này.

1. Người dùng và mục tiêu công việc.

Người dùng là sinh viên Đại học Công nghệ Thông tin, học ngành mạng và môn Lập trình mạng căn bản. Nhóm chọn đồ án ứng dụng nhắn tin desktop bằng C# WinForms, Visual Studio và .NET 10.

Yêu cầu ban đầu: đọc timeline, hướng dẫn chi tiết cách tạo solution/project/folder/class để nhóm triển khai thuận lợi. Sau đó người dùng tự tạo một phần project và yêu cầu kiểm tra, sửa phần sai, bổ sung phần còn thiếu trực tiếp trên máy. Phần dựng khung đã hoàn thành. Yêu cầu mới nhất chỉ là tạo file bàn giao session này, không yêu cầu triển khai tiếp chức năng chat trong lượt viết bàn giao.

Người dùng cần hướng dẫn bằng tiếng Việt, dễ hiểu, có thao tác cụ thể trong Visual Studio và giải thích nhiệm vụ từng class. Khi tiếp tục, bám theo project hiện có; không dựng lại hoặc thay thế toàn bộ khung nếu không cần.

2. Đường dẫn và môi trường.

| Nội dung | Giá trị |
|---|---|
| Workspace hiện tại | `C:\Users\long\Documents\LTMCB\Project\ChatApp.Nhom1` |
| Solution | `ChatApp.Nhom1.slnx` |
| Timeline đã đọc | `C:\Users\long\source\repos\TimelineNhom1 - Kế hoạch.pdf` |
| IDE đã xác minh lúc dựng khung | Visual Studio Community 2026, phiên bản 18.10.3 |
| SDK đã xác minh | .NET SDK 10.0.401 |
| Workload đã xác minh | .NET desktop development |
| Shell | PowerShell trên Windows |
| Múi giờ | Asia/Bangkok |

Workspace ban đầu của cuộc trò chuyện là `C:\Users\long\source\repos`, nhưng project thực tế nằm ở đường dẫn Documents bên trên. Không sửa nhầm các project WinForms khác trong LTMCB.

3. Phạm vi đồ án và timeline đã đọc.

Đề giảng viên yêu cầu:

- Đăng ký/đăng nhập Email, SĐT, Google.
- Bạn bè: tìm kiếm, thêm, xóa.
- Chat cá nhân realtime, đã xem, đang nhập.
- Nhóm và quản lý thành viên.
- File/ảnh/video, xem trước, giới hạn dung lượng.
- Thông báo tin mới và yêu cầu kết bạn.

Nhóm dự kiến dùng username/password, chat text và lịch sử, thời gian gửi, phân biệt tin của mình/người khác, online/offline, một server TCP trung tâm và nhiều client, bạn bè, nhóm, typing/seen, emoji, popup WinForms, tìm tin nhắn, lưu session tùy chọn và bảo mật.

Timeline PDF một trang đã được trích văn bản và xem ảnh để kiểm tra. Deadline là 01/11/2026. Snapshot trong PDF ngày 16/10/2026 ghi tiến độ 13%, 5/31 việc bắt buộc đã xong và 6 việc trễ hạn; đây là số liệu trong PDF, không phải tiến độ được cập nhật sau khi dựng khung.

| Giai đoạn | Nội dung/mốc trong PDF |
|---|---|
| Thiết kế | Kiến trúc, database, chia module, protocol JSON; 08–15/10 |
| Giao diện | Login/Register, chat chính, bạn bè, nhóm; 10–18/10 |
| Code | TCP nhiều client, tài khoản, bạn bè, chat riêng, trạng thái, lịch sử, nhóm |
| Chat riêng/lịch sử | Chủ yếu 16–21/10 |
| Nhóm phía server/database | 22–23/10 |
| Test và fix | 26–29/10 |
| Báo cáo, slide, demo, tập trình bày | 28–31/10 |
| Backlog | Typing/Seen, notification, tìm tin, file/ảnh dưới 5 MB, emoji, mã hóa |

Phân công thể hiện trong PDF: Long phụ trách kiến trúc; Bảo Chương phụ trách database và nhiều phần UI/server/tài khoản/bạn bè; Lê Anh phụ trách protocol, UI bạn bè/nhóm. Nhiều công việc sau đó còn ghi N/A. Không coi các gợi ý triển khai của assistant là phân công mới đã được cả nhóm chấp thuận.

Các điểm đã nhắc nhóm cần đối chiếu với cô:

- Username/password chưa đáp ứng các phương thức Email/SĐT/Google.
- Tính năng đưa vào backlog chưa chắc được phép bỏ theo đề bài; video chưa rõ trong timeline.
- Popup khi ứng dụng đang chạy khác thông báo nhận được khi ứng dụng đã đóng.
- SHA256 trực tiếp không phù hợp để lưu mật khẩu. Đề xuất PasswordService dùng cơ chế chuyên dụng như PasswordHasher; chưa cài package hoặc triển khai.
- Đề xuất TLS qua SslStream cho đường truyền; chưa triển khai. TLS không phải mã hóa đầu cuối.

4. Kiến trúc đã chọn và tạo.

| Project | Loại | Target | Vai trò |
|---|---|---|---|
| ChatApp.Client | Windows Forms App | net10.0-windows | Giao diện, sau này kết nối server |
| ChatApp.Server | Console App | net10.0 | Sau này lắng nghe TCP, xử lý nghiệp vụ/database |
| ChatApp.Shared | Class Library | net10.0 | DTO, cấu hình mặc định, protocol dùng chung |

Client và Server đều có ProjectReference tới Shared. Shared không tham chiếu ngược. Client và Server không tham chiếu trực tiếp nhau. Chỉ server được truy cập database.

Luồng mục tiêu:

```text
Form → Client Service → ChatConnection → TCP
     → ClientConnection → RequestDispatcher → Server Service → Repository
```

Các quy tắc thiết kế đã ghi lại:

- Một client giữ một kết nối dùng chung; không mở kết nối riêng cho mỗi Form.
- Một vòng đọc cho mỗi kết nối; tuần tự hóa việc ghi để không xen kẽ gói.
- Dùng I/O bất đồng bộ; đưa cập nhật control về luồng UI bằng Invoke/InvokeAsync.
- Server xác định danh tính từ kết nối đã xác thực, không tin senderId do client tự khai.
- Server tạo thời gian UTC; client đổi sang giờ địa phương để hiển thị.
- Form xử lý giao diện; Service xử lý chức năng; Repository đọc/ghi database.
- Seen trong nhóm cần trạng thái theo từng thành viên, không chỉ một IsSeen chung.

5. Các lỗi tìm thấy và đã sửa.

| Ban đầu | Đã xử lý |
|---|---|
| Solution chỉ có `<Solution />` | Thêm đủ ba project |
| Client/Server thiếu tham chiếu Shared | Thêm ProjectReference vào hai csproj |
| File LoginForm.cs còn class/constructor Form1 | Đổi tên thành LoginForm ở cả hai partial file |
| LoginForm chưa thống nhất namespace với thư mục | Đặt namespace ChatApp.Client.Forms |
| Program.cs gọi LoginForm nhưng thiếu namespace | Thêm using ChatApp.Client.Forms |
| Hai thành phần Controls kế thừa Form | Chuyển sang UserControl, sửa kích thước/metadata Designer |
| Shared có Class1 mẫu | Xóa Class1, bổ sung các class có nhiệm vụ cụ thể |
| Server chỉ in Hello World | Thay bằng thông báo trạng thái khung và chờ Enter |

Hai UserControl là MessageBubbleControl và FriendListControl. Metadata UserControl đã được ghi vào csproj và cập nhật trong csproj.user tại thời điểm sửa. Các file .resx được giữ nguyên. Ba form RegisterForm, MainForm, CreateGroupForm được đối chiếu với backup và không bị thay đổi trong lượt dựng khung.

Trong lúc thực hiện, hai file LoginForm có lần được lưu lại làm namespace trở về bản cũ; đã đọc lại, đồng bộ và kiểm tra thành công. Khi Visual Studio báo thay đổi từ bên ngoài, cần Reload để tránh lưu đè nội dung cũ trong editor. Đây là lưu ý khi phối hợp chỉnh file, không phải lỗi còn tồn tại.

Có backup nguồn trước khi sửa tại:

```text
C:\Users\long\AppData\Local\Temp\ChatApp.Nhom1-before-scaffold-20261017-001212
```

Backup chứa 26 file nguồn ban đầu, không gồm bin/obj/.vs. Đã kiểm tra thư mục còn tồn tại lúc viết bàn giao, nhưng đây là thư mục tạm và có thể bị dọn sau này.

6. Cấu trúc code hiện có.

```text
ChatApp.Nhom1/
├── ChatApp.Nhom1.slnx
├── README.md
├── .gitignore
├── database/
│   └── README.md
├── handoff/
│   ├── architecture.md
│   ├── getting-started.md
│   ├── protocol.md
│   ├── test-cases.md
│   └── docs/
│       └── session-handoff-2026-10-17.md
├── ChatApp.Client/
│   ├── Program.cs
│   ├── Forms/                 LoginForm, RegisterForm, MainForm, CreateGroupForm
│   ├── Controls/              MessageBubbleControl, FriendListControl
│   ├── Networking/            ChatConnection
│   ├── Services/              AuthService, ChatService, FriendService, GroupService
│   ├── State/                 ClientSession
│   └── Resources/             README.md
├── ChatApp.Server/
│   ├── Program.cs
│   ├── Networking/            ChatServer, ClientConnection, RequestDispatcher
│   ├── Services/              AuthService, ChatService, FriendService,
│   │                         GroupService, PresenceService
│   ├── Data/                 DatabaseConnectionFactory
│   │   └── Repositories/     UserRepository, MessageRepository,
│   │                         FriendRepository, GroupRepository
│   ├── Models/               README.md
│   └── Security/             PasswordService
└── ChatApp.Shared/
    ├── Configuration/        NetworkDefaults
    ├── Protocol/             Packet, PacketTypes, PacketSerializer
    ├── Transport/            FrameCodec
    └── Contracts/
        ├── Auth/             LoginRequest, RegisterRequest, LoginResponse
        ├── Users/            UserSummaryDto
        ├── Chat/             README.md
        ├── Friends/          README.md
        └── Groups/           README.md
```

Tên không có phần mở rộng trong cây là class .cs. Forms/Controls có kèm Designer.cs và resx.

Các class Networking, Services, Repositories, PasswordService, PacketSerializer, FrameCodec hiện chỉ có mô tả và TODO. Chưa có code mở socket, phân tích gói, xác thực hoặc truy vấn database. Không suy luận rằng có tên file nghĩa là tính năng đã làm xong.

Các kiểu dữ liệu đã khai báo:

- LoginRequest: Username, Password.
- RegisterRequest: Username, Password, DisplayName.
- LoginResponse: Success, ErrorMessage nullable, User nullable.
- UserSummaryDto: UserId, Username, DisplayName; không có password hash.
- ClientSession: CurrentUser và IsLoggedIn suy ra từ CurrentUser; chỉ là trạng thái trong bộ nhớ, không phải cơ chế xác thực phía server hay lưu session xuống ổ đĩa.
- Packet: Version=1, Type, RequestId nullable, Payload kiểu JsonElement nullable.
- PacketTypes: tên gói ping, login, register và error; chưa có handler.

Các ID DTO dự kiến truyền dưới dạng chuỗi; server ánh xạ với kiểu khóa database. Đây là lựa chọn cho khung giao tiếp, chưa đối chiếu schema thực tế và không yêu cầu tự đổi cột database.

7. Protocol dự kiến, chưa triển khai.

NetworkDefaults hiện khai báo Host="127.0.0.1", Port=5000, MaxPacketBytes=1024*1024 (1 MiB cho phần JSON). Chưa có bộ đọc gói thực thi giới hạn này.

JSON UTF-8, tên trường camelCase:

```json
{
  "version": 1,
  "type": "auth.login.request",
  "requestId": "b074c049-9392-4b70-9e86-60f55ca1489a",
  "payload": {
    "username": "long",
    "password": "example-only"
  }
}
```

Đóng gói trên TCP theo thiết kế:

```text
[4 byte độ dài JSON, số nguyên có dấu big-endian][JSON UTF-8]
```

Độ dài hợp lệ dự kiến 1..MaxPacketBytes. Tính theo byte UTF-8, không tính theo số ký tự. Đọc đủ header rồi đủ payload; một ReadAsync không tương ứng một tin nhắn. Kiểm tra giới hạn trước khi cấp phát. Mất kết nối giữa gói khác việc đóng kết nối trước gói mới.

Request dùng GUID chuỗi; response giữ nguyên RequestId; sự kiện chủ động từ server dùng null. Cần bổ sung DTO lỗi và DTO phản hồi đăng ký khi triển khai dispatcher. Tên loại gói có sẵn không có nghĩa chức năng đã hoạt động.

FrameCodec dự kiến làm việc trên Stream để hỗ trợ NetworkStream và SslStream sau này. Giới hạn một gói JSON không phải giới hạn toàn bộ file; khi làm truyền file cần chốt kích thước file và cách chia phần/luồng truyền riêng.

8. Database và giao diện chưa hoàn thành.

PDF ghi thiết kế database đã xong nhưng chưa có schema/script thực tế trong workspace được cung cấp cho assistant. Chưa chọn hoặc cài provider database, chưa tạo bảng, chưa chạy SQL. Cần lấy schema của nhóm trước khi triển khai Models/Repositories.

Bốn Form và hai UserControl còn trống. Client.Program chạy LoginForm. Server.Program chỉ in cấu hình dự kiến và thông báo chưa mở TCP, rồi chờ Enter. Chưa có luồng Login → Main, chưa có nút đăng nhập giả, chưa có nghiệp vụ trả thành công giả.

Chưa thực hiện: TCP, login/register thực tế, database, chat, lịch sử, bạn bè, nhóm, online/offline, typing/seen, notification, file/ảnh/video, TLS, Google/SĐT/Email, lưu phiên, tìm tin nhắn.

9. Kết quả kiểm tra trong lượt dựng khung.

- `dotnet build .\ChatApp.Nhom1.slnx` đã thành công: đủ ba project, 0 warning, 0 error.
- Server Console đã chạy và kết thúc bằng Enter.
- Chương trình smoke test tạm bên ngoài repo đã khởi tạo LoginForm, RegisterForm, MainForm, CreateGroupForm, tạo window handle thành công.
- Đã nhúng MessageBubbleControl và FriendListControl vào MainForm, kiểm tra chúng là UserControl và có handle.
- Đã kiểm tra tham chiếu Shared, các đường dẫn trong solution và tính nguyên vẹn các resx/ba form không sửa.
- Chưa trực tiếp thao tác Visual Studio Designer hoặc F5 trong IDE. Không khẳng định đã kiểm thử giao diện bằng mắt.
- Không có kiểm thử mạng/database vì chưa triển khai.
- Smoke test chỉ là công cụ kiểm tra tạm, không thêm project test vào solution.

Kết quả smoke test cuối cùng:

```text
PASS Form: ChatApp.Client.Forms.LoginForm
PASS Form: ChatApp.Client.Forms.RegisterForm
PASS Form: ChatApp.Client.Forms.MainForm
PASS Form: ChatApp.Client.Forms.CreateGroupForm
PASS embedded UserControl: ChatApp.Client.Controls.MessageBubbleControl
PASS embedded UserControl: ChatApp.Client.Controls.FriendListControl
PASS Shared reference: port 5000
```

10. Những thay đổi hiện trạng được phát hiện khi viết bàn giao.

Lúc assistant dựng khung, bốn tài liệu được đặt ở docs/ và hiển thị trong Solution Folder. Khi đọc lại để bàn giao:

- Bốn tài liệu hiện nằm trực tiếp trong handoff/.
- handoff/docs/ là nơi người dùng yêu cầu đặt bản tóm tắt này.
- Solution hiện chỉ liệt kê ba project, không còn các Solution Folder tài liệu. Điều này không tự nó là lỗi build.
- README gốc vẫn liên kết docs/getting-started.md, docs/architecture.md, docs/protocol.md, docs/test-cases.md; các liên kết tương đối này hiện không khớp vị trí mới. Cần rà lại các tham chiếu tới docs/ trong code và tài liệu khi đồng bộ đường dẫn.
- Lượt viết bàn giao không di chuyển tài liệu, không sửa README/solution và không tự khôi phục cấu trúc cũ.
- Git hiện đã tồn tại: branch main, commit gần nhất lúc kiểm tra là bf3cfc0, thông điệp "first commit". README còn câu chưa khởi tạo Git, nay đã cũ.
- Trước khi thêm bản bàn giao, git status ghi bốn file docs/*.md bị xóa, handoff/ và note.ini chưa được theo dõi. Không tự stage/commit/reset các thay đổi này; cần đọc lại status ở session mới.
- Có note.ini và ChatApp.Client/ChatApp.Client.csproj.Backup.tmp; assistant chưa đọc nội dung hoặc sửa hai file này. Không tự xóa vì chúng không thuộc thay đổi dựng khung đã xác minh.

Các liên kết tương đối từ chính file bàn giao này:

- [README gốc](../../README.md)
- [Kiến trúc](../architecture.md)
- [Hướng dẫn Visual Studio](../getting-started.md)
- [Protocol](../protocol.md)
- [Kiểm thử](../test-cases.md)
- [Ghi chú database](../../database/README.md)

11. Cách mở và chạy tiếp.

Trong Visual Studio, mở ChatApp.Nhom1.slnx, Reload nếu file thay đổi bên ngoài. Chọn Build Solution. Đặt ChatApp.Client làm Startup Project để chạy LoginForm; đặt ChatApp.Server để chạy Console. Shared không phải ứng dụng khởi động.

```powershell
Set-Location 'C:\Users\long\Documents\LTMCB\Project\ChatApp.Nhom1'
dotnet build .\ChatApp.Nhom1.slnx
dotnet run --project .\ChatApp.Client\ChatApp.Client.csproj
dotnet run --project .\ChatApp.Server\ChatApp.Server.csproj
```

Hai lệnh run dùng ở hai terminal riêng nếu muốn chạy đồng thời. Hiện mở được hai chương trình không có nghĩa chúng đã kết nối. Khi triển khai LAN, client cần IP máy server; 127.0.0.1 luôn chỉ máy hiện tại.

12. Hướng tiếp tục đã đề xuất, chưa phải công việc đã làm.

Ưu tiên tùy yêu cầu tiếp theo của người dùng:

1. Nếu chỉnh tài liệu: đồng bộ các liên kết theo vị trí handoff hiện tại, cập nhật ghi chú Git; bảo toàn thay đổi đang có.
2. Nếu bắt đầu chức năng mạng: thống nhất protocol với nhóm, làm PacketSerializer/FrameCodec, server và hai client trao đổi ping; kiểm tra chia/gộp gói, tiếng Việt, ngắt kết nối, giới hạn độ dài và ghi đồng thời.
3. Nếu bắt đầu UI: thiết kế Login/Register trong Designer, đặt tên control rõ ràng, chưa giả lập xác thực thành công.
4. Khi có schema: triển khai repository và đăng ký/đăng nhập xuyên suốt UI → TCP → server → database.
5. Sau đó chat riêng/lịch sử, online/offline, bạn bè, nhóm/quyền thành viên.
6. Bổ sung typing/seen, thông báo và các chức năng theo phạm vi đã chốt với giảng viên.

Không tự chọn database, viết lại schema chưa đọc, hoặc coi toàn bộ backlog là phần được phép bỏ. Không biến bước dựng khung thành triển khai toàn bộ ứng dụng khi người dùng chưa yêu cầu phạm vi đó.

Lời nhắn có thể dùng để mở session mới:

> Hãy đọc handoff/docs/session-handoff-2026-10-17.md trong project ChatApp.Nhom1 để nắm bối cảnh. Project hiện mới hoàn thành khung, chưa có chức năng TCP/database/chat. Hãy kiểm tra file hiện tại và các thay đổi Git trước khi tiếp tục theo yêu cầu mới của tôi.
