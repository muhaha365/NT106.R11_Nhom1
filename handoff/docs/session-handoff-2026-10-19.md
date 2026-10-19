# Bàn giao session ngày 19/10/2026 — Kết nối TCP đầu tiên

Workspace: `C:\Users\long\Documents\LTMCB\Project\ChatApp.Nhom1`.

Tài liệu này tổng hợp nội dung trao đổi và code đã đọc trực tiếp khi bàn giao. Tiếp nối [bản ngày 18/10](session-handoff-2026-10-18.md). Chi tiết thay đổi từng file nằm trong [bản ghi thay đổi code](../changes/code-changes-2026-10-19.md).

**Trạng thái quan trọng:** bạn đã tự triển khai bước kết nối TCP. Server nhận nhiều kết nối trên cùng máy; Client có nút kết nối. Phần phát hiện server ngắt kết nối mới được hướng dẫn, **chưa có trong code hiện tại**. Chưa triển khai gửi/nhận tin nhắn hoặc database.

## 1. Mục tiêu và cách hỗ trợ

Trong session, bạn yêu cầu:

1. Đọc handoff ngày 18/10 và cấu trúc project để đề xuất kiến trúc client–server dùng TCP, chỉ rõ code nằm ở đâu.
2. Hướng dẫn tự viết code trên cấu trúc hiện có.
3. Giải thích logic vì bạn có thể hiểu khi đọc code nhưng khó hình dung khi không nhìn code.
4. Giải thích và hướng dẫn khắc phục việc Client vẫn hiện “Đã kết nối TCP” sau khi Server dừng.
5. Lưu tổng hợp session và ghi lại những thay đổi code thực tế thành hai file Markdown.

Tiếp tục dùng cách xưng hô **bạn/tôi**. Ưu tiên hướng dẫn để bạn tự thực hiện, từng phần nhỏ giống bài lab. Trình bày luồng chạy trước rồi mới nối với câu lệnh; tránh đưa nhiều khái niệm mới cùng lúc.

Tôi đã thừa nhận bài đầu đưa vào khá nhiều chi tiết: `async/await`, hủy tác vụ, timeout, xử lý lỗi, giải phóng tài nguyên và vòng đời Form. Chúng hữu ích nhưng khiến luồng chính khó thấy. Không yêu cầu bạn nhớ toàn bộ cú pháp hoặc viết lại từ trí nhớ để chứng minh đã hiểu.

## 2. Kiến trúc tiếp tục sử dụng

Giữ cấu trúc đơn giản đã thống nhất ngày 18/10; chưa thêm Services, Repositories hoặc RequestDispatcher.

```text
Client A ── TCP ──┐
Client B ── TCP ──┼── Server ── đọc/ghi ── Database (chưa triển khai)
Client C ── TCP ──┘
```

- Chỉ cần một project Client; chạy nhiều lần sẽ có nhiều client.
- Client và Server là hai chương trình độc lập. Client gửi tin cho nhau thông qua Server khi đã làm chức năng chuyển tiếp.
- Shared là thư viện dùng chung, không chạy riêng và không phải trạm trung chuyển.
- Client và Server đều tham chiếu Shared; Shared không tham chiếu ngược.
- Chỉ Server truy cập database.

| Project | Loại | Target hiện tại |
|---|---|---|
| ChatApp.Client | WinForms | net10.0-windows |
| ChatApp.Server | Console | net10.0 |
| ChatApp.Shared | Class Library | net10.0 |

```text
ChatApp.Client/
  Program.cs
  ClientConnection.cs
  Forms/LoginForm.cs (+ Designer, resx)
  Forms/RegisterForm.cs
  Forms/MainForm.cs
ChatApp.Server/
  Program.cs
  ChatServer.cs
  ConnectedClient.cs
  Database.cs
ChatApp.Shared/
  Packet.cs
  PacketIO.cs
database/
  README.md
```

Vai trò mục tiêu:

- `ClientConnection`: giữ kết nối, gửi/nhận và đóng kết nối phía Client.
- Các Form: lấy dữ liệu từ control, gọi chức năng kết nối/gửi yêu cầu, hiển thị kết quả.
- `ChatServer`: nhận client, quản lý các kết nối, sau này xử lý yêu cầu và chuyển tin.
- `ConnectedClient`: giữ một kết nối ở phía Server; sau này thêm danh tính đã xác thực và vòng nhận.
- `Database`: các hàm C# truy vấn dữ liệu. Thư mục `database/` dành cho schema/script và hướng dẫn.
- `Packet`: định nghĩa gói. `PacketIO`: đọc/ghi đủ gói trên luồng byte.

Các đề xuất như bảng tra cứu `UserId → ConnectedClient`, mỗi tài khoản một phiên đăng nhập, `switch (packet.Type)`, lưu tin rồi chuyển tiếp, phân biệt đã lưu/đã nhận/đã xem đều là hướng phát triển, chưa có code thực hiện.

## 3. Code hiện tại đã có gì?

### Server

- `Program.cs` tạo `CancellationTokenSource`, bắt `Console.CancelKeyPress`, đặt `e.Cancel = true` rồi yêu cầu hủy. Gọi `ChatServer.RunAsync(5000, cancellation.Token)` và bắt `SocketException`.
- `ChatServer.RunAsync` dùng `TcpListener(IPAddress.Loopback, port)`, gọi `Start`, lặp `AcceptTcpClientAsync(cancellationToken)`.
- Mỗi kết nối mới được bọc trong `ConnectedClient`, thêm vào `List<ConnectedClient>` và in địa chỉ đầu bên kia.
- Khi hủy, xử lý `OperationCanceledException`; `finally` dừng listener và dispose các client.
- `ConnectedClient : IDisposable` giữ thuộc tính `TcpClient` và đóng nó trong `Dispose()`.

Danh sách hiện chỉ giữ các kết nối đã được nhận, chưa tự loại bỏ client đã rời. Không coi đó là danh sách người dùng online chính xác. Server chưa đọc dữ liệu hoặc xác thực tài khoản.

### Client

- `ClientConnection : IDisposable` giữ `_tcpClient` nullable.
- `ConnectAsync(host, port)` đóng kết nối cũ, tạo `TcpClient`, đặt timeout 5 giây và kết nối bất đồng bộ. Nếu lỗi, dispose socket, đặt biến về null và ném lại lỗi.
- Có `Disconnect()` và `Dispose()`; **chưa có `ReceiveLoopAsync`**.
- `LoginForm` có trường `_connection = new ClientConnection()` và nút thực tế mang tên **`btn_connect`**, sự kiện **`btn_connect_Click`**.
- Bấm nút sẽ khóa nút, kết nối tới `127.0.0.1:5000`, đổi chữ thành “Đã kết nối TCP” và hiện MessageBox khi thành công. Khi kết nối thất bại, hiển thị lỗi và bật lại nút.
- `OnFormClosed` dispose kết nối.
- `Client/Program.cs` vẫn gọi `Application.Run(new LoginForm())`.

Hiện kết nối thuộc vòng đời LoginForm. Khi triển khai đăng nhập và chuyển sang MainForm, cần tổ chức dùng chung một kết nối qua các Form, có thể chuyển quyền sở hữu lên Program. Đó là bước tiếp theo, chưa được thực hiện.

### Shared và database

- `Packet` đã có `Version = 1`, `Type`, `RequestId`, `Payload` kiểu `JsonElement?` từ trước.
- `PacketIO` vẫn là TODO: dự kiến `[4 byte độ dài big-endian][JSON UTF-8]`, đọc đủ từng phần và kiểm tra kích thước.
- `Database.cs` vẫn rỗng ngoài comment. `database/` chỉ có README, chưa có script SQL.
- RegisterForm và MainForm vẫn chỉ khởi tạo giao diện; chưa có logic đăng ký/chat.

## 4. Cách giải thích luồng kết nối cho bạn

```text
SERVER                              CLIENT
Mở cổng 5000
Chờ kết nối...                      Người dùng bấm Kết nối
                                    Kết nối tới 127.0.0.1:5000
Nhận kết nối          <────────────
Giữ kết nối                         Giữ kết nối
Chờ client tiếp theo                Hiển thị kết quả
```

Server quay lại chờ không có nghĩa là đóng kết nối trước. Hai chương trình chạy độc lập nên khó hình dung hơn một bài xử lý nút bấm tuần tự.

Năm ý cần nhớ: Server mở cổng → Client bấm kết nối → Client tìm đúng IP/cổng → Server nhận và giữ kết nối → Server tiếp tục chờ client khác.

Các khái niệm phụ nên giải thích sau luồng chính:

| Khái niệm | Mục đích trong bài |
|---|---|
| async/await | Chờ thao tác mạng mà không giữ luồng UI bận |
| CancellationToken | Yêu cầu dừng thao tác đang chờ |
| try/catch/finally | Xử lý lỗi và dọn tài nguyên |
| Dispose | Đóng kết nối, giải phóng tài nguyên |
| Timeout | Giới hạn thời gian chờ kết nối |

## 5. Vấn đề đang mở: Client không biết Server đã đóng kết nối

Bạn quan sát thấy Server dừng nhưng Client vẫn hiện “Đã kết nối TCP”. Code hiện tại chỉ cập nhật chữ sau lần kết nối thành công rồi kết thúc hàm; chưa tiếp tục đọc luồng mạng để phát hiện kết nối bị đóng. Đây là giới hạn của bài mẫu đầu, không phải lỗi do bạn làm sai hướng dẫn.

Giải pháp đã hướng dẫn trong cuộc trò chuyện, **chưa được áp dụng vào file**:

1. Thêm `ReceiveLoopAsync()` trong ClientConnection.
2. Lấy `NetworkStream`, tạo buffer 1024 byte và lặp `await stream.ReadAsync(buffer.AsMemory())`.
3. Nếu số byte trả về bằng 0 với buffer có dung lượng: phía bên kia đã đóng chiều gửi và dữ liệu chờ đã được đọc hết; thoát vòng nhận.
4. Nếu có lỗi đọc, để exception truyền lên Form. Trong `finally`, đóng kết nối.
5. Trong `btn_connect_Click`, sau kết nối thành công và cập nhật trạng thái, gọi `await _connection.ReceiveLoopAsync()`.
6. Khi vòng nhận kết thúc, thông báo “Mất kết nối tới server”; khi có exception, hiển thị lỗi. Trong `finally` của sự kiện, đổi chữ nút về “Kết nối” và bật lại nút nếu Form chưa đóng.

Mẫu hướng dẫn không còn MessageBox thành công trước khi bắt đầu vòng nhận; trạng thái thành công hiển thị ở nút và tiêu đề Form.

Những điểm phải giữ khi triển khai:

- `await` vòng nhận không làm giao diện đứng. Với cách gọi từ sự kiện WinForms và await mặc định trong mẫu, phần tiếp tục cập nhật control quay lại luồng UI. Nếu đổi sang tác vụ nền hoặc bỏ bắt ngữ cảnh, phải đưa cập nhật control về UI.
- Một kết nối chỉ có một vòng nhận. Khi viết PacketIO, thay phần đọc thô bằng đọc gói trong cùng vòng đó, không chạy thêm một vòng đọc cạnh tranh.
- Buffer thô trong mẫu chưa xử lý nội dung nhận được; chỉ là bài tập trước khi có tin nhắn, không phải cách nhận chat hoàn chỉnh.
- Client thường chỉ biết mất kết nối, chưa biết chắc nguyên nhân là Server dừng hay mạng lỗi.
- Mất mạng đột ngột không có tín hiệu đóng có thể không được phát hiện ngay; Ping/Pong hoặc cơ chế kiểm tra sống sẽ được làm sau.
- Đóng Form khi đang chờ mạng cần dọn kết nối và tránh cập nhật control đã bị dispose.

## 6. Kiểm tra đã thực hiện và giới hạn

Trong phần hướng dẫn đầu session:

- Build khung ban đầu bằng `dotnet build .\ChatApp.Nhom1.slnx --no-restore` thành công, 0 warnings, 0 errors.
- Tôi tạo mẫu riêng trong thư mục tạm, build và thử hai client, dừng server, kết nối thất bại khi server đã dừng, khởi động lại và kết nối lại. Mẫu đã qua các kiểm tra đó.
- Đây là kiểm tra mẫu riêng, không phải bằng chứng giao diện và toàn bộ checkout sau khi bạn tự sửa đã được chạy thử.
- Bạn xác nhận đã code xong bước đầu và báo hiện tượng trạng thái cũ khi Server dừng.

Khi chuẩn bị hai tài liệu bàn giao:

- Đọc trực tiếp source hiện tại và đối chiếu Git.
- Build Debug bằng `dotnet build .\ChatApp.Nhom1.slnx --no-restore` thất bại ở bước sao chép `ChatApp.Client.exe` vì file đang được tiến trình khác sử dụng: MSB3026, MSB3027, MSB3021; tổng 10 warnings, 2 errors. Không tự đóng tiến trình của bạn.
- Build Release bằng `dotnet build .\ChatApp.Nhom1.slnx --no-restore --configuration Release` **thành công, 0 warnings, 0 errors**.
- Chưa chạy thử GUI, mạng LAN, database hoặc chức năng mất kết nối trong checkout ở lượt bàn giao. Build thành công không chứng minh các chức năng đó đã hoạt động.

## 7. Git và những comment cần đọc đúng

- HEAD khi kiểm tra: `14b12d1492b00004dcabddbadf9dbfb87553f3c6`, merge PR #1.
- Thay đổi kết nối TCP nằm trong commit `601e0ffef1ed3fa61292c9c774d8b6454cdf3208`, tiêu đề `feat: client-server architect`.
- Trước khi tạo tài liệu, `git status --short` chỉ có ` M note.ini`. Không có diff source C# chưa commit; không vì thế mà kết luận bạn chưa thay đổi code.
- `note.ini` không được đọc nội dung hoặc chỉnh sửa trong lượt bàn giao.
- Comment “Khung lớp: chưa triển khai chức năng” trong các lớp mạng đã lỗi thời một phần. TODO nhắc `service`, `RequestDispatcher`, hoặc `ClientConnection` phía Server thuộc thiết kế cũ. Đối tượng phía Server hiện là `ConnectedClient`.
- Không khôi phục các lớp cũ theo comment; không suy ra đã có nhận gói chỉ vì comment đề cập đến vòng nhận.

Lượt bàn giao chỉ tạo hai file Markdown theo yêu cầu; không sửa source, không stage/commit/push và không cập nhật bộ nhớ toàn cục.

## 8. Hướng tiếp tục

1. Đọc lại ClientConnection và LoginForm để biết bạn đã bổ sung xử lý mất kết nối sau bản bàn giao này hay chưa.
2. Giải thích một lần bấm nút theo luồng chạy; hướng dẫn thêm vòng nhận và cập nhật Form từng bước.
3. Thử kết nối → Ctrl+C Server → Client thông báo mất kết nối → bật lại Server → kết nối lại trên cùng cửa sổ. Thử đóng Client trong lúc chờ mạng.
4. Sau đó triển khai PacketIO và Ping/Pong; giữ đúng ranh giới gói trên TCP.
5. Tiếp tục một Server và hai Client gửi/nhận text, rồi mới thêm xác thực, database, lịch sử và các yêu cầu còn lại.

Chạy Server và Client trên các terminal riêng, tại thư mục solution:

```powershell
dotnet run --project .\ChatApp.Server\ChatApp.Server.csproj
```

```powershell
dotnet run --project .\ChatApp.Client\ChatApp.Client.csproj
```

Muốn mở hai Client thì chạy lệnh Client ở hai terminal. Đóng chương trình đang dùng output trước khi build lại Debug để tránh lỗi khóa file. Hiện listener bind Loopback nên chỉ thử cùng máy; chưa cấu hình kết nối từ máy khác trong LAN.

Lời nhắn để bắt đầu session mới:

> Hãy đọc handoff/docs/session-handoff-2026-10-19.md và handoff/changes/code-changes-2026-10-19.md. Tôi đã tự làm bước kết nối TCP nhưng cần được giải thích luồng chạy từng phần nhỏ. Khi bàn giao, Client chưa có ReceiveLoopAsync để phát hiện Server ngắt kết nối. Hãy đọc code hiện tại rồi hướng dẫn tôi tiếp tục, dùng cách xưng hô bạn/tôi.
