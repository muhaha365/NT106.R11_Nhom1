# Thay đổi code ngày 19/10/2026 — Bước kết nối Client–Server TCP

Tài liệu ghi lại những thay đổi bạn đã tự thực hiện, được xác minh bằng source hiện tại và lịch sử Git. Tổng hợp trao đổi, cách học và hướng tiếp tục nằm trong [bàn giao session](../docs/session-handoff-2026-10-19.md).

## 1. Mốc đối chiếu

- Mốc trước bước TCP: `ac67450` — `alter: architect of chat app`.
- Commit triển khai: `601e0ffef1ed3fa61292c9c774d8b6454cdf3208` — `feat: client-server architect`.
- HEAD khi kiểm tra: `14b12d1492b00004dcabddbadf9dbfb87553f3c6` — merge PR #1 từ nhánh `feat--client-server-architect`.
- Đã đọc diff `ac67450..HEAD` và nội dung file trên đĩa. Các thay đổi C# dưới đây đã có trong HEAD, không còn là thay đổi chưa commit.
- Trước khi tạo tài liệu, working tree chỉ báo ` M note.ini`. Nội dung note.ini nằm ngoài phạm vi ghi nhận chức năng; không được đọc hoặc sửa.

## 2. Thay đổi từng file

### ChatApp.Client/ClientConnection.cs

Trước: class khung với TODO.

Hiện tại:

- Thêm `using System.Net.Sockets` và triển khai `IDisposable`.
- Thêm `private TcpClient? _tcpClient` để giữ kết nối.
- Thêm `ConnectAsync(string host, int port)`:
  - Đóng kết nối cũ trước khi tạo kết nối mới.
  - Tạo TcpClient và lưu vào trường của class.
  - Tạo thời hạn kết nối 5 giây bằng CancellationTokenSource.
  - Gọi ConnectAsync bất đồng bộ.
  - Nếu lỗi: dispose socket, đặt trường về null và ném lại exception cho Form.
- Thêm `Disconnect()` để dispose kết nối nếu có và đặt trường về null.
- Thêm `Dispose()` gọi Disconnect.

Kết quả: Client có code thiết lập và chủ động đóng kết nối TCP. Chưa có vòng nhận, gửi gói, sự kiện mất kết nối hay tự kết nối lại.

### ChatApp.Client/Forms/LoginForm.cs

Trước: constructor chỉ gọi InitializeComponent.

Hiện tại:

- Thêm `_connection`, một đối tượng ClientConnection thuộc LoginForm.
- Thêm sự kiện bất đồng bộ `btn_connect_Click`.
- Khóa nút trước khi kết nối tới `127.0.0.1:5000`.
- Sau khi thành công, kiểm tra Form chưa bị dispose, đổi chữ nút thành “Đã kết nối TCP” và hiện MessageBox.
- Khi lỗi, kiểm tra vòng đời Form, hiện thông báo lỗi và bật lại nút.
- Ghi đè `OnFormClosed` để dispose kết nối rồi gọi hàm của lớp cơ sở.

Kết quả: có thao tác kết nối từ giao diện. Đây chưa phải chức năng đăng nhập; trạng thái nút chỉ phản ánh lần kết nối thành công trước đó, chưa theo dõi kết nối hiện còn sống hay không.

### ChatApp.Client/Forms/LoginForm.Designer.cs

- Thêm Button tên chính xác `btn_connect`, chữ hiển thị “Kết nối”.
- Vị trí `(348, 252)`, kích thước `(75, 23)` trong snapshot đang đọc.
- Gắn `btn_connect.Click += btn_connect_Click` và thêm vào Controls.
- Có SuspendLayout/ResumeLayout và AutoScaleDimensions `(7F, 15F)`.

Giữ tên control này khi tiếp tục hướng dẫn; tên `btnConnect` trong mẫu ban đầu không phải tên thực tế bạn đã dùng.

### ChatApp.Client/Forms/LoginForm.resx

Diff có thay đổi khoảng trắng trong comment mô tả schema ResX. Không thấy thêm dữ liệu tài nguyên chức năng trong diff này; không coi đây là một tính năng mới.

### ChatApp.Server/Program.cs

Trước: chỉ in tên ứng dụng, thông báo chưa triển khai mạng rồi chờ Enter.

Hiện tại:

- Tạo CancellationTokenSource.
- Đăng ký Console.CancelKeyPress: đặt `e.Cancel = true` và gọi Cancel.
- In hướng dẫn nhấn Ctrl+C để dừng Server.
- Tạo ChatServer và chờ `RunAsync(5000, cancellation.Token)`.
- Bắt SocketException và in lỗi mạng.

Kết quả: điểm khởi động Server thực sự gọi phần lắng nghe TCP và có luồng dừng bằng Ctrl+C.

### ChatApp.Server/ChatServer.cs

Trước: class khung với TODO.

Hiện tại:

- Thêm `RunAsync(int port, CancellationToken cancellationToken)`.
- Tạo TcpListener bind `IPAddress.Loopback` và danh sách `List<ConnectedClient>`.
- Gọi Start, in địa chỉ lắng nghe.
- Lặp AcceptTcpClientAsync để tiếp nhận từng kết nối mới.
- Mỗi kết nối được bọc trong ConnectedClient, đưa vào danh sách, in RemoteEndPoint.
- Xử lý OperationCanceledException khi token được hủy.
- Trong finally: dừng listener, dispose các client và in “Server đã dừng”.

Kết quả: có thể giữ nhiều kết nối đã nhận và tiếp tục chờ client khác. Listener hiện chỉ nhận trên cùng máy. Danh sách chưa tự loại bỏ client đã rời; chưa phải danh sách online. Chưa có xử lý gói hoặc chuyển tiếp tin.

File còn `using System.Diagnostics.CodeAnalysis` chưa thấy được dùng; không tự sửa trong lượt tạo tài liệu.

### ChatApp.Server/ConnectedClient.cs

Trước: class khung với TODO.

Hiện tại:

- Thêm `using System.Net.Sockets` và triển khai IDisposable.
- Thêm thuộc tính chỉ đọc `TcpClient`.
- Constructor nhận TcpClient từ ChatServer và lưu lại.
- Dispose đóng TcpClient.

Kết quả: mỗi đối tượng giữ một kết nối ở phía Server. Chưa có UserId, xác thực, vòng đọc hoặc hàm gửi gói.

## 3. Những phần chưa thay đổi chức năng

| Thành phần | Trạng thái khi bàn giao |
|---|---|
| Client/Program.cs | Vẫn chạy LoginForm; chưa quản lý kết nối dùng chung qua nhiều Form |
| RegisterForm, MainForm | Chưa có logic đăng ký hoặc chat |
| Shared/Packet.cs | Giữ cấu trúc Version, Type, RequestId, Payload đã có |
| Shared/PacketIO.cs | Vẫn TODO; chưa đóng gói hoặc đọc đủ gói TCP |
| Server/Database.cs | Chưa có truy vấn |
| database/ | Chỉ có README; chưa có script SQL |
| Solution và project references | Vẫn ba project .NET 10; Client và Server tham chiếu Shared |

Chưa triển khai đăng nhập, đăng ký, lưu tin, chat riêng/nhóm, lịch sử, danh sách online, TLS hoặc truyền file.

## 4. Đã hướng dẫn nhưng chưa có trong code

**Phát hiện Server ngắt kết nối:**

- Đề xuất thêm `ClientConnection.ReceiveLoopAsync()` dùng NetworkStream.ReadAsync.
- Khi đọc được 0 byte thì kết thúc vòng nhận; khi lỗi thì chuyển exception cho Form; dọn kết nối trong finally.
- Đề xuất để LoginForm chờ vòng nhận sau khi kết nối, hiển thị mất kết nối và bật lại nút.
- Kiểm tra source lúc bàn giao và ngay trước khi tạo file vẫn không có hàm này hoặc lời gọi tương ứng.

Vì vậy, lỗi giao diện giữ chữ “Đã kết nối TCP” khi Server đã dừng **vẫn chưa được khắc phục trong snapshot này**. Không đưa phần hướng dẫn vào danh sách chức năng đã hoàn thành.

Mẫu vòng nhận đã trao đổi mới là bài tập phát hiện kết nối đóng, chưa xử lý dữ liệu nhận được. Khi làm chat phải thay phần đọc thô bằng PacketIO trong cùng một vòng nhận.

## 5. Kết quả kiểm tra

| Kiểm tra | Kết quả và phạm vi |
|---|---|
| Đọc source và Git diff | Xác nhận các thay đổi trên có trong checkout và commit triển khai |
| Build Debug hiện tại: `dotnet build .\ChatApp.Nhom1.slnx --no-restore` | Không hoàn tất vì ChatApp.Client.exe đang bị tiến trình khác sử dụng; MSB3026/MSB3027/MSB3021, 10 warnings và 2 errors |
| Build Release hiện tại: `dotnet build .\ChatApp.Nhom1.slnx --no-restore --configuration Release` | Thành công, 0 warnings và 0 errors |
| Chạy thử mẫu TCP riêng trước đó trong session | Qua bài hai client, dừng server, thất bại khi server tắt, khởi động lại và kết nối lại; không phải kiểm thử GUI của checkout hiện tại |
| Chạy thử checkout trong lượt bàn giao | Không chạy GUI/LAN/database; chưa kiểm thử cách sửa mất kết nối vì chưa được áp dụng |

Không tự đóng tiến trình Client đang chạy để giải phóng file. Khi cần build lại Debug, đóng ứng dụng dùng output đó trước.

## 6. Phạm vi thao tác tạo bản ghi

Tôi chỉ đọc code/Git, chạy kiểm tra build và tạo hai tài liệu ngày 19/10 trong handoff/docs và handoff/changes. Không sửa code C#, Designer, resx, cấu hình hoặc note.ini; không stage, commit hay push.

Một số comment cũ trong class mạng vẫn nói “chưa triển khai chức năng” hoặc nhắc service/RequestDispatcher. Những comment này chưa phản ánh đầy đủ code hiện tại; ưu tiên đọc thân hàm và bản ghi này khi tiếp tục.
