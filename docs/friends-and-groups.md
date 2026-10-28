# UI bạn bè và chat nhóm

Hai task gồm giao diện WinForms và phần client gửi/nhận theo [protocol JSON](protocol.md). Nghiệp vụ xác thực, bạn bè, nhóm và database phía server được ghép ở task khác. Không có tài khoản hoặc danh sách người dùng giả trong ứng dụng.

Giao diện dùng nền sáng, điểm nhấn indigo, danh sách dạng thẻ có avatar chữ cái và chỉ báo online. Tin nhắn nhóm hiển thị thành bong bóng: tin của bạn ở bên phải, tin của thành viên khác ở bên trái. Nội dung vẫn là control văn bản có thể chọn chữ và sao chép bằng Ctrl+C; lăn chuột hoặc dùng thanh cuộn để xem lịch sử. Thanh điều hướng hỗ trợ bàn phím và tự xuống hàng khi thiếu chỗ.

Với tin nhắn dài hơn khung chat, vùng cuộn theo con trỏ khi di chuyển hoặc chọn văn bản bằng bàn phím.

## Mở giao diện

1. Build solution với SDK .NET 10 và workload Windows desktop.
2. Chạy ChatApp.Client, chọn **Demo chat** ở LoginForm.
3. MainForm hiển thị ba tab **Bạn bè**, **Lời mời**, **Tìm người** ở cột trái; vùng **Chat nhóm** có danh sách nhóm, lịch sử, thành viên và ô gửi tin.
4. Kết nối tới địa chỉ/cổng server. Tab **Chat chung (demo)** tiếp tục dùng gói `chat` với server hiện tại.

Kết nối TCP chưa phải đăng nhập. MainForm mặc định chưa có user nên khóa các thao tác bạn bè/nhóm. Server hiện chỉ xử lý broadcast demo; các thao tác nghiệp vụ chỉ hoạt động khi handler tương ứng và login thật được bổ sung. Không nhập một user ID tùy ý để giả phiên đăng nhập.

## Điểm ghép với luồng đăng nhập

`ProtocolClient` dùng `ClientConnection`/`PacketIO` hiện có nhưng sở hữu **một vòng nhận duy nhất**. Nó ghép response bằng RequestId và phát event riêng. Khi dùng ProtocolClient, không gọi thêm ReceiveAsync ở LoginForm/MainForm hoặc control.

Luồng login cần dùng ProtocolClient để gửi auth.login.request; sau response thành công, bàn giao **chính kết nối đó** và User công khai cho MainForm. Ví dụ trong một event handler UI, sau khi kết nối đã mở:

```csharp
using ChatApp.Client;
using ChatApp.Client.Forms;
using ChatApp.Shared.Contracts;

// connection là ProtocolClient đang dùng cho login; username/password đến từ form.
var login = await connection.RequestAsync<LoginResult>(
    "auth.login.request", new { Username = username, Password = password });

var chat = new MainForm(connection, login.User);
chat.Show();

// DTO mẫu để module auth khai báo cùng phần contracts khi triển khai.
public sealed record LoginResult(UserSummary User);
```

Đoạn trên là điểm ghép cho module auth, chưa thay thế handler đăng nhập trong ứng dụng. MainForm sở hữu kết nối được bàn giao và dispose khi đóng. Không dispose kết nối ở LoginForm sau khi đã bàn giao; một kết nối chỉ có một chủ sở hữu vòng đời.

Không gửi trực tiếp model UserData vì model đó chứa PasswordHash. UserSummary chỉ có Id, Username, DisplayName, IsOnline. Server phải xác thực và kiểm tra quyền cho mọi request; việc ẩn/khóa nút ở UI không thay thế kiểm tra server.

## Các thao tác đã nối ở client

| Giao diện | Request |
|---|---|
| Tải bạn bè/lời mời | friend.list.request với Accepted/Pending, lấy đủ các trang |
| Tìm người | user.search.request, lấy đủ các trang |
| Kết bạn | friend.add.request |
| Chấp nhận/từ chối lời mời nhận được | friend.respond.request |
| Xóa bạn có xác nhận | friend.remove.request |
| Tải/tạo nhóm | group.list.request, group.create.request |
| Danh sách/thêm/xóa thành viên | group.members.request, group.member.add.request, group.member.remove.request |
| Lịch sử nhóm | chat.history.request với PeerId=null, GroupId đã chọn, Limit=10 |
| Gửi tin nhóm | chat.send.request với ReceiverId=null, GroupId đã chọn |

Control dùng response thực từ server rồi mới cập nhật kết quả. Mỗi request có GUID riêng và timeout mặc định 10 giây. Timeout nghĩa là kết quả chưa xác định, không tự retry các thao tác có thể tạo dữ liệu trùng. Sau lỗi mất mạng, phiên UI bị xóa và cần login lại; NOT_AUTHENTICATED cũng làm mất hiệu lực kết nối dùng chung.

Nếu tạo nhóm hoặc đổi thành viên bị mất phản hồi, cửa sổ khóa thao tác lặp và yêu cầu đóng để tải lại danh sách. Nếu gửi tin nhóm bị mất phản hồi, nút **Đồng bộ tin nhắn** tải lịch sử mới nhất trước khi cho gửi tiếp; bản nháp vẫn được giữ để người dùng kiểm tra kết quả đã lưu.

`FriendsPanel.FriendSelected` là điểm nối cho phần chat cá nhân sau này; hiện nút **Chọn bạn** hiển thị thông tin người được chọn, không gửi tin cá nhân.

MainForm chuyển các event presence.changed.event, friend.changed.event, group.changed.event và chat.message.event về UI thread trước khi đưa vào control. Event và history được gộp theo Message.Id; đổi nhóm không hiển thị response/event của nhóm cũ. Bản nháp giữ riêng từng nhóm. Khi bị xóa khỏi nhóm, UI xóa lịch sử đang mở, bỏ nhóm khỏi danh sách và khóa gửi.

Chỉ chủ nhóm nhìn thấy nút quản lý thành viên; không xóa chủ nhóm. Các quy tắc thực vẫn cần được server kiểm tra. Gửi tin bằng Enter, xuống dòng bằng Shift+Enter. Content có giới hạn 2.000 Unicode scalar và 4.000 byte UTF-8 theo protocol.

## Những gì backend cần cung cấp

- Login thật gắn User.Id vào kết nối; không dùng ID kết nối demo làm user ID.
- Response đúng Type/RequestId và Payload `{ Success, Data, Error }`; lỗi protocol chung dùng system.error.response.
- Handler friend/group/history và các event tương ứng; lưu tin thành công trước khi trả kết quả gửi.
- Phân trang/cursor, quan hệ bạn bè và thành viên nhóm, quyền truy cập lịch sử.
- UTC trên đường truyền, bảo vệ thông tin xác thực và TLS khi dùng tài khoản thật giữa các máy.

Không có thay đổi ở ChatApp.Server hoặc database trong phần UI này.

## Build dự án

```powershell
dotnet build .\ChatApp.Nhom1.slnx
```
