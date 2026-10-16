Quy ước protocol phiên bản 1 cho nhóm thống nhất trước khi triển khai TCP. PacketSerializer, FrameCodec và RequestDispatcher hiện mới là class khung; tài liệu này chưa phải các hành vi đã được thực thi.

Dữ liệu gói là JSON mã hóa UTF-8, tên thuộc tính camelCase.

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

| Trường | Quy ước |
|---|---|
| version | Số nguyên, hiện là 1; server từ chối phiên bản không hỗ trợ |
| type | Chuỗi loại gói, đối chiếu PacketTypes |
| requestId | Yêu cầu dùng chuỗi GUID mới; phản hồi giữ nguyên; sự kiện server chủ động gửi dùng null |
| payload | Dữ liệu theo DTO của từng chức năng; có thể null cho ping |

Phản hồi đăng nhập thành công:

```json
{
  "version": 1,
  "type": "auth.login.response",
  "requestId": "b074c049-9392-4b70-9e86-60f55ca1489a",
  "payload": {
    "success": true,
    "errorMessage": null,
    "user": {
      "userId": "123",
      "username": "long",
      "displayName": "Long"
    }
  }
}
```

Đăng nhập thất bại vẫn dùng auth.login.response với success=false, user=null và errorMessage phù hợp. LoginResponse chỉ chứa thông tin người dùng, chưa có token lưu phiên. Server sẽ gắn danh tính với kết nối sau khi xác thực.

Đối với lỗi protocol/loại gói/quyền truy cập chung, dự kiến dùng system.error.response:

```json
{
  "version": 1,
  "type": "system.error.response",
  "requestId": "b074c049-9392-4b70-9e86-60f55ca1489a",
  "payload": {
    "code": "UNAUTHORIZED",
    "message": "Bạn cần đăng nhập."
  }
}
```

DTO lỗi này cần bổ sung khi triển khai dispatcher. Nếu gói không thể đọc an toàn hoặc không xác định được requestId, không cố phản hồi theo dữ liệu hỏng; ghi log an toàn và đóng kết nối khi cần.

Các tên gói khởi đầu đã khai báo trong PacketTypes:

| Yêu cầu | Phản hồi | Payload |
|---|---|---|
| system.ping.request | system.ping.response | null |
| auth.login.request | auth.login.response | LoginRequest / LoginResponse |
| auth.register.request | auth.register.response | RegisterRequest / DTO phản hồi sẽ bổ sung |
| — | system.error.response | DTO lỗi sẽ bổ sung |

Khi thêm chat/bạn bè/nhóm, bổ sung tên gói và DTO trong cùng thay đổi rồi thông báo cho người làm phía còn lại. Không dùng LoginResponse cho mọi loại phản hồi chỉ vì có các trường tương tự.

Trên TCP, mỗi gói có định dạng:

```text
[4 byte độ dài JSON, số nguyên có dấu big-endian][JSON UTF-8]
```

Giới hạn hiện đề xuất trong NetworkDefaults là 1.048.576 byte cho phần JSON, không tính bốn byte header. Độ dài hợp lệ từ 1 đến MaxPacketBytes.

FrameCodec cần thực hiện:

1. Đọc đủ bốn byte header.
2. Đọc số byte độ dài theo big-endian và kiểm tra giới hạn trước khi cấp phát.
3. Đọc đủ phần JSON, có thể cần nhiều lần đọc.
4. Chuyển JSON thành Packet bằng PacketSerializer, sau đó kiểm tra loại gói/dữ liệu.
5. Khi peer đóng trước gói mới, kết thúc bình thường. Khi peer đóng giữa header hoặc payload, coi là gói bị cắt.
6. Truyền CancellationToken và xử lý ngắt kết nối. Các tác vụ ghi trên cùng kết nối phải được tuần tự hóa.

Một lần ReadAsync không bảo đảm tương ứng một lần gửi; một lần đọc cũng có thể nhận dữ liệu của nhiều gói. Không giả định buffer 1024/4096 byte luôn đủ cho một tin.

PacketSerializer cần dùng System.Text.Json với cấu hình camelCase thống nhất. Không serialize mật khẩu vào log. Các trường khởi tạo rỗng trong DTO không thay thế cho validation ở server.

ID trên đường truyền dùng chuỗi để server có thể ánh xạ khóa int/GUID của database hiện có. Đây là quy ước truyền dữ liệu; không yêu cầu đổi kiểu cột database. Khi có schema, kiểm tra chuyển đổi và định dạng ID không hợp lệ.

Các timestamp khi bổ sung dùng DateTimeOffset UTC / ISO 8601. Server xác định người gửi từ kết nối đã đăng nhập và tự cấp ID/thời gian cho tin nhắn.

Giới hạn gói JSON khác giới hạn dung lượng file. Khi bổ sung file dưới 5 MB theo timeline, nhóm phải chốt số byte chính xác, chia file thành nhiều phần hoặc thiết kế luồng truyền riêng; không nhét nguyên file vào một gói vượt giới hạn. Server kiểm tra dung lượng, quyền nhận và tên tệp.

TLS chưa có trong khung. Chỉ thử nghiệm tài khoản giả trong giai đoạn TCP chưa được bảo vệ. Triển khai bảo vệ đường truyền trước khi dùng mật khẩu thực.
