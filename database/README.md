Timeline ghi thiết kế database đã hoàn thành, nhưng thư mục project hiện chưa có schema hoặc script SQL của nhóm.

Đặt bản thiết kế/script hiện có tại đây trước khi triển khai Server/Data. Khung chưa tạo, sửa hoặc chạy bất kỳ database nào.

Việc cần thống nhất:

1. Công nghệ và phiên bản database.
2. Tên bảng/cột, kiểu khóa chính và khóa ngoại.
3. Dữ liệu User, tin nhắn/cuộc trò chuyện, bạn bè, nhóm và thành viên.
4. Quy trình yêu cầu kết bạn: chờ/chấp nhận/từ chối.
5. Trạng thái đọc theo từng thành viên nếu có Seen.
6. Cách lưu file: metadata trong database và nội dung ở vùng lưu trữ do server quản lý.
7. Cách cấu hình kết nối trên máy chạy server và thứ tự chạy script.

Chỉ cài package database vào ChatApp.Server khi đã chọn công nghệ. Client không giữ thông tin đăng nhập database và không truy vấn trực tiếp.

DTO trong Shared dùng ID chuỗi trên đường truyền; repository ánh xạ với khóa int/GUID hoặc kiểu khác trong schema. Không cần đổi schema chỉ để khớp tên DTO.

Không commit mật khẩu, dữ liệu người dùng thật hoặc backup database có dữ liệu nhạy cảm. Không tự chạy lại script tạo/cập nhật tích lũy trên database đã có dữ liệu. README này sẽ được bổ sung hướng dẫn cụ thể khi có schema.
