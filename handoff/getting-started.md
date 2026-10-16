Bạn đã tạo đúng ba loại project và target .NET 10. Các phần đã được sửa trong khung:

| Vấn đề ban đầu | Đã sửa |
|---|---|
| ChatApp.Nhom1.slnx chỉ có <Solution /> | Thêm đủ Client, Server, Shared và tài liệu |
| Client/Server chưa tham chiếu Shared | Thêm ProjectReference trong hai csproj |
| LoginForm.cs vẫn khai báo class Form1 | Đổi cả class, constructor, phần Designer và namespace sang LoginForm |
| Program.cs không import namespace Forms | Thêm using ChatApp.Client.Forms |
| Hai file trong Controls kế thừa Form | Đổi sang UserControl, sửa Designer và metadata của Visual Studio |
| Shared còn Class1 mẫu | Thay bằng folder và các class dùng chung có tên rõ ràng |

Các form khác và các tài nguyên .resx đã được giữ lại. Giao diện hiện vẫn trống để nhóm tự thiết kế.

Cách tiếp tục:

1. Mở ChatApp.Nhom1.slnx. Nếu Visual Studio đang mở bản cũ, chấp nhận Reload; tránh lưu đè phiên bản cũ đang nằm trong editor.
2. Nhấn Ctrl + Shift + B để build.
3. Chuột phải ChatApp.Client → Set as Startup Project.
4. Mở Forms/LoginForm.cs → View Designer (Shift + F7).
5. Bật View → Toolbox và View → Properties Window.
6. Kéo Label, TextBox, Button để thiết kế UI theo phân công. Ví dụ đặt tên txtUsername, txtPassword, btnLogin, btnOpenRegister. TextBox mật khẩu dùng UseSystemPasswordChar=true.
7. Code sự kiện nằm trong LoginForm.cs. Chưa có xử lý đăng nhập; chỉ gọi AuthService khi service và server đã thực sự được triển khai.
8. Với Controls/MessageBubbleControl.cs và FriendListControl.cs, mở bằng Designer như bình thường. Chúng là thành phần nhúng trong Form.
9. Nhấn F5 để kiểm tra cửa sổ LoginForm. Cửa sổ trống hiện tại là đúng với giai đoạn khung.
10. Đặt ChatApp.Server làm startup project để chạy Console. Console chưa nhận kết nối TCP.

Tạo thêm file đúng loại:

| Cần tạo | Thao tác trong folder tương ứng |
|---|---|
| Cửa sổ | Add → New Item → Windows Form |
| Thành phần giao diện dùng lại | Add → New Item → User Control |
| Class chức năng hoặc DTO | Add → Class |
| Thư mục code thật | Add → New Folder bên trong project |
| Nhóm tài liệu ở cấp solution | Add → New Solution Folder; tạo file thật trên ổ đĩa rồi Add Existing Item |

Đổi tên file không phải lúc nào cũng đổi class bên trong. Khi đổi tên Form, kiểm tra class, constructor, partial class trong Designer, namespace và Program.cs. Nên dùng refactor/rename của Visual Studio để giữ nhất quán.

Các class khung có mô tả và TODO. Đọc mô tả trước khi viết code. Không cần thêm interface, tầng kiến trúc hay package nếu chưa có nhu cầu cụ thể.

Thứ tự triển khai đề xuất:

| Bước | Kết quả cần đạt |
|---|---|
| 1 | PacketSerializer và FrameCodec gửi/nhận được ping giữa server và hai client |
| 2 | Ghép Register/Login từ Form → Client service → TCP → Server service → Database |
| 3 | Chat riêng và lưu/load lịch sử, kiểm tra quyền |
| 4 | Online/offline và bạn bè, yêu cầu kết bạn |
| 5 | Nhóm, thành viên và quyền quản lý |
| 6 | Typing/Seen, thông báo và các tính năng theo phạm vi đã chốt |

Khi ghép hai chương trình, có thể vào cấu hình startup của solution và chọn chạy cả Server lẫn Client. Đợi server thực sự lắng nghe rồi mới kết nối. Để test nhiều client, mở thêm bản Client từ thư mục build.

Timeline đã có người làm kiến trúc, database và protocol; các việc còn để N/A cần được nhóm phân công. Đặc biệt chốt Contracts trong Shared trước khi mỗi người tự viết gửi/nhận.

Database chưa được cung cấp trong thư mục này. Đưa schema đã thiết kế vào database và đọc database/README.md trước khi viết repository.
