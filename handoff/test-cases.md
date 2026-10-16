Danh sách kiểm tra để nhóm sử dụng trong các giai đoạn sau. Những mục chưa triển khai không được đánh dấu đã đạt chỉ vì solution build thành công.

Đã kiểm tra ngày 17/10/2026: build solution thành công với 0 lỗi, 0 cảnh báo; chạy Console Server và thoát bằng Enter; dùng chương trình kiểm tra tạm bên ngoài solution để khởi tạo bốn Form, tạo window handle và nhúng hai UserControl vào MainForm. Các kiểm tra này đã đạt. Chưa thao tác trực tiếp trong Visual Studio Designer và chưa kiểm tra mạng/database vì các chức năng đó chưa được triển khai.

Kiểm tra khung:

- dotnet build ChatApp.Nhom1.slnx phải build cả Shared, Server và Client.
- F5 với Client làm startup project mở được LoginForm.
- Bốn Form khởi tạo được; hai thành phần trong Controls là UserControl.
- Server in trạng thái khung; chưa có cổng TCP lắng nghe.
- Shared không tham chiếu Client/Server; không có database connection string ở client.

Kiểm tra sau khi có TCP/protocol:

- Hai client cùng kết nối, mỗi client nhận đúng phản hồi ping.
- Header bị chia thành nhiều lần đọc.
- JSON có dấu tiếng Việt/emoji bị chia ở giữa ký tự UTF-8.
- Hai gói đến liên tiếp trong cùng đợt đọc.
- Độ dài 0, số âm hoặc vượt giới hạn bị từ chối trước khi cấp phát.
- Ngắt kết nối giữa header, giữa payload và trước gói mới.
- Hủy tác vụ và dừng server giải phóng socket.
- Nhiều yêu cầu gửi đồng thời không làm xen kẽ byte; response ghép đúng requestId.
- JSON hỏng, loại gói không hỗ trợ và version không hỗ trợ không làm sập server.

Kiểm tra sau khi có tài khoản:

- Đăng ký mới, username trùng, trường rỗng, dữ liệu quá dài.
- Đăng nhập đúng/sai mật khẩu; tài khoản không tồn tại.
- Password hash được lưu thay vì mật khẩu gốc.
- Không có mật khẩu/token trong log.
- Client chưa đăng nhập không xem lịch sử, gửi tin hay quản lý nhóm được.
- Client tự khai senderId của người khác không mạo danh được.
- Đăng xuất và mất kết nối làm mất quyền của phiên cũ.
- Chính sách nhiều phiên của một tài khoản được thực hiện nhất quán.

Kiểm tra sau khi có chat/bạn bè/nhóm:

- Chat hai chiều giữa hai tài khoản, nội dung tiếng Việt và emoji.
- Tin của mình/người khác hiển thị đúng phía, thời gian đúng múi giờ.
- Mở lại cuộc trò chuyện lấy đúng lịch sử, có phân trang khi nhiều tin.
- Gửi cho người offline có hành vi được quy định rõ; không báo đã nhận khi chưa có xác nhận.
- Tìm người dùng; tự kết bạn; yêu cầu trùng; chấp nhận/từ chối/xóa.
- Người ngoài nhóm không xem lịch sử hoặc gửi vào nhóm được.
- Thành viên thường không tự nâng quyền hay xóa người trái phép.
- Typing có thời gian hết hạn; Seen chỉ gửi khi người dùng thực sự xem cuộc trò chuyện.
- Seen của một thành viên không đánh dấu mọi thành viên nhóm đều đã xem.
- Mất mạng/reconnect không nhân đôi tin khi cơ chế gửi lại được bổ sung.

Kiểm tra sau khi có tệp và thông báo:

- Tệp rỗng, đúng giới hạn, vượt giới hạn, truyền dở rồi ngắt mạng.
- Tên tệp chứa đường dẫn không được phép ghi ra ngoài vùng lưu tệp.
- Người không có quyền không tải được tệp.
- Xem trước ảnh/video xử lý định dạng không hỗ trợ.
- Thông báo mới và yêu cầu kết bạn không lặp vô hạn.
- Kiểm tra riêng khi app mở, thu nhỏ và đã thoát; không coi popup trong app là thông báo khi app đã đóng.
