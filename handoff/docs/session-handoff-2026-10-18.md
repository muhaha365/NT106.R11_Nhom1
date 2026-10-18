# Bàn giao session ngày 18/10/2026 — ChatApp.Nhom1

Đọc tài liệu này trước khi tiếp tục. Quyết định mới nhất là **giữ khung đơn giản, gần với bài lab WinForms**, thay cho kiến trúc nhiều lớp ở session trước. Không tự khôi phục Services, Repositories, RequestDispatcher hoặc hệ thống DTO chỉ vì các tài liệu cũ có nhắc tới.

Tài liệu ghi lại nội dung trao đổi và hiện trạng file được đọc trực tiếp khi bàn giao. Chưa build hoặc chạy thử khung sau khi rút gọn trong session này. Có tên class không có nghĩa chức năng đã được triển khai.

## 1. Bối cảnh và cách hỗ trợ phù hợp

- Bạn là sinh viên năm 2 ngành Mạng máy tính tại UIT, đang học lập trình mạng bằng C#.
- Bạn đã làm Lab 1 trong `C:\Users\long\source\repos\NT106_Lab1`, biết tạo solution/project/Form, kéo thả Button/TextBox và viết sự kiện xử lý logic cơ bản.
- Tên tài liệu lab bạn cung cấp: `Lab1 - Introduction to C# n Winform Application - V2019 (1).pdf`. Session này không mở lại PDF, không khẳng định đã kiểm tra nội dung lab trong lượt này.
- Bạn thấy kiến trúc nhiều thư mục/lớp quá khó tiếp cận. Cần giải thích từng bước, gắn trực tiếp với file, Form và nút bấm quen thuộc.
- Xưng hô với bạn bằng **bạn/tôi**, không dùng cách xưng hô khác.
- Khi bạn yêu cầu hướng dẫn, ưu tiên hướng dẫn thao tác cụ thể để bạn tự thực hiện. Không tự thay toàn bộ code hoặc dựng một kiến trúc mới.
- Bạn đã tự làm theo hướng dẫn thu gọn và xác nhận khung dễ hiểu hơn. Khi bàn giao, cấu trúc trên đĩa đã phản ánh thay đổi này.
- Bạn kết thúc buổi học, sẽ yêu cầu hỗ trợ tính năng ở session sau. Yêu cầu cuối chỉ là lưu bản tổng hợp vào `handoff/docs`; không triển khai chức năng mới.

## 2. Phạm vi đề bài và phạm vi khởi đầu

Danh sách ban đầu:

1. Đăng nhập/đăng ký bằng Email, SĐT, Google.
2. Bạn bè: tìm kiếm, thêm, xóa.
3. Chat cá nhân: real-time, đã xem, đang nhập.
4. Chat nhóm: tạo nhóm, quản lý thành viên.
5. Gửi file/ảnh/video, xem trước, giới hạn dung lượng.
6. Thông báo tin mới và yêu cầu kết bạn.

Sau khi trao đổi, bạn muốn ưu tiên chức năng cơ bản thể hiện bản chất lập trình mạng. Phạm vi khởi đầu đã đề xuất:

| Làm trước | Để sau phần cơ bản |
|---|---|
| Đăng ký/đăng nhập bằng tên tài khoản, mật khẩu | Email, Google, OTP SĐT |
| Danh sách tài khoản online | Quan hệ bạn bè và lời mời kết bạn |
| Nhắn tin riêng | Đã xem, đang nhập |
| Lưu và xem lịch sử | File, ảnh, video |
| Một phòng chat chung | Tạo nhóm, quản lý thành viên, thông báo đẩy |

Đây là thứ tự/phạm vi học và triển khai ban đầu, **không có bằng chứng giảng viên đã cho phép bỏ các yêu cầu còn lại**. Không suy ra ngưỡng qua môn hoặc tỷ lệ hoàn thành từ đề xuất này.

## 3. Cấu trúc hiện tại đã xác minh

Workspace: `C:\Users\long\Documents\LTMCB\Project\ChatApp.Nhom1`.

```text
ChatApp.Nhom1/
├── ChatApp.Nhom1.slnx
├── ChatApp.Client/
│   ├── ChatApp.Client.csproj
│   ├── Program.cs
│   ├── ClientConnection.cs
│   └── Forms/
│       ├── LoginForm.cs
│       ├── RegisterForm.cs
│       └── MainForm.cs
├── ChatApp.Server/
│   ├── ChatApp.Server.csproj
│   ├── Program.cs
│   ├── ChatServer.cs
│   ├── ConnectedClient.cs
│   └── Database.cs
├── ChatApp.Shared/
│   ├── ChatApp.Shared.csproj
│   ├── Packet.cs
│   └── PacketIO.cs
├── database/
│   └── README.md
└── handoff/
    ├── architecture.md
    ├── protocol.md
    ├── getting-started.md
    ├── test-cases.md
    └── docs/
        ├── session-handoff-2026-10-17.md
        └── session-handoff-2026-10-18.md
```

Cây trên lược bỏ file phụ, tài nguyên Form, bin/obj và cấu hình IDE. Giữ `.Designer.cs` và `.resx` đi cùng ba Form. Không đổi `.slnx` thành `.sln` chỉ để giống sơ đồ minh họa.

| Project | Loại/target hiện tại | Tham chiếu |
|---|---|---|
| Client | WinForms, `net10.0-windows` | Shared |
| Server | Console, `net10.0` | Shared |
| Shared | Class Library, `net10.0` | Không tham chiếu ngược |

Client và Server là hai chương trình chạy độc lập, giao tiếp qua mạng. Shared là thư viện, không chạy riêng. Chỉ Server truy cập database.

## 4. Nhiệm vụ mỗi file trong thiết kế đơn giản

| File | Nhiệm vụ dự kiến |
|---|---|
| Client/Forms/LoginForm | Lấy dữ liệu đăng nhập, gửi yêu cầu, nhận kết quả rồi mở MainForm |
| Client/Forms/RegisterForm | Lấy dữ liệu đăng ký, gửi yêu cầu |
| Client/Forms/MainForm | Danh sách online, khung chat, nhập/gửi/hiển thị tin |
| Client/ClientConnection | Giữ một kết nối dùng chung, kết nối/ngắt, gửi/nhận dữ liệu |
| Server/Program | Khởi động chương trình server |
| Server/ChatServer | Lắng nghe TCP, nhận client, xử lý yêu cầu và chuyển tiếp tin |
| Server/ConnectedClient | Gắn một kết nối với tài khoản sau khi xác thực |
| Server/Database | Các hàm đọc/ghi database: tạo tài khoản, kiểm tra đăng nhập, lưu tin, lấy lịch sử |
| Shared/Packet | Cấu trúc một gói dữ liệu |
| Shared/PacketIO | Chuyển dữ liệu và gửi/đọc đủ một gói trên TCP |

Code sự kiện nút bấm vẫn có thể viết trong Form như bài lab. Dùng các control quen thuộc: TextBox, Button, ListBox, RichTextBox. Chưa cần custom control, nhiều service hay nhiều DTO. Khi chức năng đã rõ và file khó quản lý mới cân nhắc tách thêm.

Luồng mục tiêu:

```text
MainForm A → ClientConnection → TCP → ChatServer
                                    ├→ Database (khi đã làm lưu trữ)
                                    └→ ClientConnection B → MainForm B
```

Server có thể dùng `switch (packet.Type)` để xử lý từng loại yêu cầu. Những tên `Login`, `PrivateMessage`, `RoomMessage` được dùng làm ví dụ trong trao đổi, chưa phải protocol đã triển khai/chốt đầy đủ.

## 5. Trạng thái code khi bàn giao

- `Client/Program.cs` gọi `Application.Run(new LoginForm())`.
- Ba Form vẫn là khung cơ bản; chưa thấy các control chat/đăng nhập được khai báo trong Designer khi kiểm tra. Chưa có luồng đăng nhập thật.
- `Server/Program.cs` chỉ in tên ứng dụng, thông báo chưa triển khai mạng và chờ Enter. Không mở cổng TCP.
- `ClientConnection`, `ChatServer`, `ConnectedClient`, `Database`, `PacketIO` vẫn là class khung/TODO.
- `Packet` đã có `Version = 1`, `Type`, `RequestId` nullable và `Payload` kiểu `JsonElement?`; namespace đã là `ChatApp.Shared`.
- `PacketIO` có comment dự kiến dùng 4 byte độ dài big-endian rồi JSON UTF-8, kiểm tra kích thước và đọc đủ dữ liệu; chưa có code thực hiện.
- Chưa có truy vấn database, socket hoạt động, chat, lịch sử, danh sách online, xác thực, TLS, truyền tệp hay thông báo.
- `database/` hiện chỉ có README, chưa thấy script schema. Không tự cho rằng đã có `CreateDatabase.sql` hoặc database hoạt động.
- Các ProjectReference tới Shared còn đúng. Metadata hai UserControl cũ trong Client.csproj đã được gỡ.

Một số comment vẫn nhắc tên cũ, cần hiểu đúng khi tiếp tục:

- `ClientConnection.cs` còn nhắc phát sự kiện tới service, dù Services đã được bỏ.
- `ChatServer.cs` còn nhắc tạo `ClientConnection` ở server; tên hiện tại là `ConnectedClient`.
- `ConnectedClient.cs` còn nhắc `RequestDispatcher`, class này đã được bỏ. Xử lý yêu cầu hiện dự kiến đặt trong ChatServer.

Đây là comment cũ, chưa phải lỗi thực thi vì code mạng chưa được viết. Không tái tạo các lớp cũ chỉ để làm theo comment.

## 6. Những thay đổi và giới hạn xác minh

Trong session, tôi hướng dẫn bạn chuyển/đổi tên file và bỏ các thư mục của kiến trúc cũ. Tôi không trực tiếp thực hiện refactor. Khi viết bàn giao, đã đọc lại cây file, các class chính, Program và csproj để xác minh khung rút gọn có trên đĩa.

- Không chạy build, F5, Designer hoặc test mạng/database trong session này.
- Kết quả build/smoke test trong bản ngày 17/10 thuộc khung cũ, không phải bằng chứng khung mới đã được kiểm thử.
- Đã hướng dẫn tạo bản sao `ChatApp.Nhom1_Backup` nằm ngoài thư mục dự án trước khi sửa; chưa kiểm tra bản sao thực sự tồn tại.
- `git status --short` hiện có nhiều file cũ bị xóa, các file mới ở cấp project chưa được theo dõi, Client.csproj và Server/Program.cs bị sửa. Đây phù hợp với quá trình thu gọn, không tự reset/khôi phục.
- `note.ini` cũng đang thay đổi; chưa đọc hoặc sửa nội dung.
- Lượt bàn giao chỉ thêm tài liệu này, không stage/commit hoặc sửa code.

## 7. Kiểm tra repo tham khảo trong session

Bạn đã yêu cầu đối chiếu tính năng repo `https://github.com/tatsuya-akira/Chat-E2EE` với sáu nhóm yêu cầu. Tôi tải repo về thư mục tạm và đọc mã nguồn tại commit `1518cf67bfd73c22b34b7c33fbcae3e2cca3e564`, không chạy ứng dụng hay xác minh dịch vụ đang triển khai.

| Phần | Kết luận từ mã nguồn |
|---|---|
| Xác thực | Có luồng email và Google; hai hàm OTP SĐT ném NotImplementedException |
| Bạn bè | Có tìm tài khoản, tạo chat; có bảng CONTACTS nhưng chưa thấy luồng kết bạn/chấp nhận/xóa quan hệ |
| Chat cá nhân | Có SignalR, lưu tin và read receipts; chưa thấy typing; phần tải lại IsRead có nguy cơ không phản ánh đúng người nhận đã đọc |
| Nhóm | Có tạo, xem thành viên, xóa thành viên, rời nhóm; chưa thấy thêm vào nhóm đã tồn tại hoặc phân quyền trưởng nhóm đầy đủ |
| File/ảnh/video | Chưa tìm thấy luồng truyền, xem trước, kiểm tra dung lượng |
| Thông báo | Có sự kiện tải lại danh sách chat khi đang kết nối; chưa thấy đầy đủ thông báo yêu cầu kết bạn/Windows/khi app đóng |

Repo tham khảo còn có code E2EE, tin tự hủy và gọi thoại WebRTC. Không lấy repo này làm chuẩn bắt buộc cho cấu trúc nhóm bạn. Không suy ra điểm số, tỷ lệ qua môn hoặc việc nhóm tác giả đã qua môn. Các kết luận trên chỉ áp dụng snapshot mã nguồn đã đọc.

## 8. Hướng tiếp tục ở session sau

Chờ yêu cầu mới của bạn. Nếu bạn muốn bắt đầu lập trình mạng, tiến hành từng bước nhỏ:

1. Đọc file hiện tại và kiểm tra build khung rút gọn; sửa vấn đề cần thiết nếu có.
2. Giải thích IP, port, Client/Server bằng ví dụ hai chương trình trên cùng máy; không nhồi toàn bộ kiến trúc vào một lượt.
3. Làm Server nhận kết nối từ Client.
4. Thống nhất một cấu trúc gói đơn giản, triển khai PacketIO và giải thích vì sao một lần đọc TCP chưa chắc nhận đủ tin.
5. Làm một Server và hai Client gửi/nhận tin text. Bước này chưa cần đăng nhập hoặc database; không giả lập đăng nhập thành công rồi coi là đã làm xác thực.
6. Sau đó thêm tài khoản, chat riêng, online, lịch sử, phòng chung theo từng yêu cầu cụ thể.

Các nguyên tắc cần giữ khi viết phần thực tế:

- Một Client dùng chung một kết nối qua các Form; mỗi kết nối một vòng đọc, các lần ghi được tuần tự hóa.
- Không chặn luồng giao diện khi chờ mạng; cập nhật control trên luồng UI.
- Server kiểm tra dữ liệu, quyền và danh tính từ kết nối đã xác thực.
- TCP là luồng byte, cần đọc đủ gói và xử lý mất kết nối/giới hạn kích thước.
- Khi làm đăng nhập thực tế: băm mật khẩu bằng cơ chế chuyên dụng và bảo vệ đường truyền; không lưu mật khẩu nguyên văn.
- Chỉ Server truy cập database. Đọc schema nhóm cung cấp trước khi viết truy vấn; chưa mặc định đã chốt DBMS.
- Phân biệt thông báo khi app đang mở với cơ chế hoạt động khi app đã đóng.

Lệnh kiểm tra khi tiếp tục (chưa chạy trong lượt bàn giao):

```powershell
dotnet build .\ChatApp.Nhom1.slnx
dotnet run --project .\ChatApp.Server\ChatApp.Server.csproj
dotnet run --project .\ChatApp.Client\ChatApp.Client.csproj
```

Hai lệnh run dùng hai terminal riêng. Trong Visual Studio, chọn đúng Startup Project trước F5; Shared không là Startup Project. Mở được hai chương trình hiện chưa chứng minh đã kết nối.

## 9. Thứ tự ưu tiên tài liệu

- Quyết định rút gọn ngày 18/10 trong tài liệu này thay thế hướng tổ chức nhiều lớp ngày 17/10.
- README, `handoff/architecture.md`, `handoff/protocol.md` và các tài liệu cũ chưa được đồng bộ trong lượt này. Các tên folder/class cũ, DTO và NetworkDefaults trong đó có thể không còn tồn tại.
- Bản ngày 17/10 vẫn hữu ích cho lịch sử, nhưng thông tin deadline, công cụ, timeline, build và phân công trong đó là snapshot cũ; chưa xác minh lại hôm nay.
- Không tự cập nhật bộ nhớ toàn cục: yêu cầu lần này là tạo tài liệu trong workspace.

Lời nhắn để bắt đầu session mới:

> Hãy đọc handoff/docs/session-handoff-2026-10-18.md. Tôi đã chọn khung đơn giản gồm Client WinForms, Server Console và Shared; code mạng/database vẫn chưa triển khai. Hãy hướng dẫn tôi từng bước nhỏ giống bài lab, dùng cách xưng hô bạn/tôi và tiếp tục trên khung hiện tại.
