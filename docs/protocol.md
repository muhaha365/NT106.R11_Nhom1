# Protocol gửi nhận message JSON

Phiên bản thiết kế 1 · Lê Anh · 27/10/2026

Tài liệu thống nhất cấu trúc yêu cầu, phản hồi và sự kiện để client/server triển khai theo cùng hợp đồng. Giữ `Packet` hiện có, không thay đổi cách demo chat đang chạy.

## 1. Trạng thái hiện tại

- [Packet.cs](../ChatApp.Shared/Packet.cs) có `Version`, `Type`, `RequestId`, `Payload`.
- [PacketIO.cs](../ChatApp.Shared/PacketIO.cs) serialize bằng System.Text.Json mặc định, gửi một JSON trên một dòng và đọc bằng ReadLineAsync.
- [ChatServer.cs](../ChatApp.Server/ChatServer.cs) chỉ nhận `Type = "chat"`, Payload là chuỗi, rồi chuyển tiếp tới các kết nối khác. ID như Client 0 là ID kết nối thử nghiệm, không phải user đã xác thực.
- [MainForm.cs](../ChatApp.Client/Forms/MainForm.cs) gửi/nhận gói chat, chưa ghép response nghiệp vụ bằng RequestId.

**Các gói nghiệp vụ bên dưới là thiết kế cần triển khai tiếp.** Runtime chưa xử lý auth/friend/group/history, chưa kiểm tra schema hay giới hạn gói. Schema không tự bổ sung các hành vi đó cho PacketIO.

Tài liệu này thay mô tả cũ về header độ dài 4 byte, camelCase và ID dạng chuỗi bằng quy ước phù hợp code/model hiện có: JSON theo dòng, PascalCase và ID nghiệp vụ dạng int. Không trộn hai cách đóng khung trên cùng kết nối.

File đi kèm:

- [protocol.schema.json](protocol.schema.json): JSON Schema Draft 2020-12 cho 40 loại gói, bao gồm gói demo.
- [protocol.examples.jsonl](protocol.examples.jsonl): 45 mẫu gói độc lập bao phủ mọi loại gói. Không gửi toàn bộ file vào một socket như một kịch bản: nhiều dòng là response/event chỉ server được gửi.

## 2. Cách truyền gói trên TCP

| Thuộc tính | Quy định thiết kế |
|---|---|
| Mã hóa | UTF-8 không BOM |
| Một khung | Một JSON object gọn, kết thúc bằng LF hoặc CRLF |
| Tên trường | PascalCase, phân biệt hoa/thường, khớp Packet.cs |
| Serialize | WriteIndented = false; dùng serializer, không nối JSON thủ công |
| Giới hạn đề xuất | Tối đa 65.536 byte JSON UTF-8, không tính dấu kết thúc dòng |
| Đọc/ghi | Một vòng đọc và một khóa/hàng ghi dùng chung cho mỗi kết nối |

StreamWriter.WriteLineAsync hiện gửi CRLF trên Windows; StreamReader.ReadLineAsync đọc được LF và CRLF. Xuống dòng trong Content được escape thành `\n`; dấu nháy và gạch chéo cũng phải qua serializer. Không serialize Payload hai lần thành chuỗi chứa JSON.

Một lần ReadAsync có thể nhận một phần gói hoặc nhiều gói. StreamReader hiện giữ buffer và trạng thái UTF-8, không coi buffer 1024/4096 byte là một tin nhắn. Khi hoàn thiện framing, phải chặn kích thước buffer trong lúc đọc: đọc hết dòng vô hạn rồi mới kiểm tra không giới hạn được bộ nhớ.

UTF-8/JSON lỗi, dòng trống hoặc gói quá trần: đóng kết nối. EOF trước gói mới là ngắt bình thường; EOF còn một dòng chưa có dấu kết thúc là gói dở, phải bỏ. ReadLineAsync hiện có thể trả dòng dở cuối stream, nên cần xử lý thêm. Các giới hạn và hành vi từ chối trên chưa được code thực thi.

## 3. Envelope chung

Tất cả gói có đủ bốn trường, không có trường envelope bổ sung:

| Trường | Kiểu và ý nghĩa |
|---|---|
| Version | Số nguyên 1 |
| Type | Tên gói trong mục 5, phân biệt hoa/thường |
| RequestId | Chuỗi GUID cho request/response; null cho event và gói demo |
| Payload | Object theo chức năng; riêng demo chat dùng chuỗi |

### Request từ client

```json
{"Version":1,"Type":"auth.login.request","RequestId":"4f083e5d-d921-4fd2-b15e-145cf2e7dc15","Payload":{"Username":"leanh","Password":"demo-only-password"}}
```

Client tạo GUID mới cho mỗi request, không tái sử dụng trong cùng kết nối. Request không có tham số vẫn gửi Payload là object rỗng. Client không gửi type .response hoặc .event lên server.

### Response từ server

Đổi hậu tố .request thành .response, giữ nguyên RequestId. Thành công có Success=true, Data là object và Error=null:

```json
{"Version":1,"Type":"auth.login.response","RequestId":"4f083e5d-d921-4fd2-b15e-145cf2e7dc15","Payload":{"Success":true,"Data":{"User":{"Id":1,"Username":"leanh","DisplayName":"Lê Anh","IsOnline":true}},"Error":null}}
```

Lỗi nghiệp vụ giữ type response tương ứng; Success=false, Data=null và Error gồm Code/Message:

```json
{"Version":1,"Type":"auth.login.response","RequestId":"4f083e5d-d921-4fd2-b15e-145cf2e7dc15","Payload":{"Success":false,"Data":null,"Error":{"Code":"INVALID_CREDENTIALS","Message":"Tên đăng nhập hoặc mật khẩu không đúng."}}}
```

Mỗi request hợp lệ nhận một response nếu kết nối còn hoạt động. Client xử lý theo Error.Code, không so sánh câu thông báo. Event có thể xen giữa response; ghép bằng RequestId, không coi gói nhận tiếp theo là kết quả request vừa gửi.

### Event từ server

RequestId luôn null; không chờ client trả lời:

```json
{"Version":1,"Type":"presence.changed.event","RequestId":null,"Payload":{"UserId":2,"IsOnline":false}}
```

### Gói demo đang chạy

```json
{"Version":1,"Type":"chat","RequestId":null,"Payload":"Xin chào từ client"}
```

Server chuyển tiếp cùng loại với chuỗi như `Client 0 gửi: Xin chào từ client`. Gói này chưa có định tuyến 1-1, xác thực hay lịch sử. Khi triển khai chat nghiệp vụ, hai phía cùng chuyển sang chat.send.request và chat.message.event; vẫn giữ demo để kiểm tra tương thích.

## 4. Dữ liệu công khai và giới hạn

Các DTO dưới đây là thiết kế; mọi trường phải có mặt, kể cả trường cho phép null. Tên trường bám model hiện có trong ChatApp.Shared/Models.

| Object | Trường |
|---|---|
| User | Id, Username, DisplayName, IsOnline |
| Message | Id, SenderId, ReceiverId nullable, GroupId nullable, Content, SendTime |
| Friendship | UserInvite, UserReceive, Status (Pending hoặc Accepted), CreatedAt |
| FriendEntry | User, Friendship; User là người còn lại trong quan hệ |
| Group | Id, GroupName, CreatedBy, CreatedAt |
| Error | Code, Message |

- ID nghiệp vụ là số nguyên 1–2.147.483.647, khớp model C# và SQL INT. Không dùng ID kết nối demo thay User.Id, không dùng 0 thay null.
- Message có đúng một đích ReceiverId/GroupId khác null. Server lấy SenderId từ session, tự cấp ID và thời gian khi lưu, không tin danh tính do client tự khai.
- Thời gian là ISO 8601 UTC có hậu tố Z, ví dụ `2026-10-27T02:20:00.000Z`. Model đang dùng DateTime.Now, SQL dùng GETDATE(): khi tích hợp cần thống nhất/chuyển đổi UTC đúng, không gắn Z trực tiếp vào giờ local.
- User công khai không chứa PasswordHash/Password. Không serialize thẳng UserData từ database ra socket.
- Username: 3–50 ký tự ASCII chữ/số/underscore, chuẩn hóa chữ thường trước lưu/tra cứu. Password: 8–128 Unicode scalar, không trim hoặc đổi hoa/thường.
- DisplayName/GroupName: trim hai đầu, không rỗng, tối đa 100 Unicode scalar. Query: trim, 1–50 Unicode scalar. Không cho ký tự điều khiển trong các trường này.
- Content: giữ nguyên, không rỗng/toàn khoảng trắng, tối đa 2.000 Unicode scalar và 4.000 byte UTF-8 sau decode; chỉ cho TAB/CR/LF trong nhóm ký tự điều khiển.
- Trần toàn gói áp dụng sau JSON escape, độc lập giới hạn Content. Nếu trang kết quả quá lớn, giảm số phần tử và trả cursor; không cắt giữa chuỗi/object.

Schema kiểm tra cấu trúc, kiểu, trường bắt buộc, GUID/UTC và một số giới hạn ký tự. Byte UTF-8, trim, quyền truy cập, quan hệ bạn bè, unique DB và thứ tự dữ liệu phải được code kiểm tra. Tên thuộc tính trùng trong JSON cần bị từ chối trước deserialize; schema không phát hiện được sau khi parser đã ghi đè chúng.

## 5. Danh mục request và response

Cột đầu là tiền tố: thêm .request khi gửi và .response khi trả. Cột kết quả mô tả Payload.Data khi Success=true. Tất cả tham số ghi trong bảng đều bắt buộc, kể cả giá trị null/mảng rỗng được phép.

### Xác thực và user

| Tiền tố | Payload request | Data thành công |
|---|---|---|
| system.ping | {} | {} |
| auth.register | {Username, Password, DisplayName} | {User}; chưa tự đăng nhập |
| auth.login | {Username, Password} | {User}; gắn danh tính với kết nối |
| auth.logout | {} | {}; sau đó đóng kết nối |
| user.search | {Query, Limit, AfterId} | {Items: User[], NextAfterId} |
| presence.list | {Limit, AfterId} | {Items: User[], NextAfterId} |

Chỉ ping/register/login dùng trước đăng nhập. Register/login trên kết nối đã xác thực trả ALREADY_AUTHENTICATED. Một user chỉ có một kết nối xác thực; login ở kết nối khác trả ALREADY_ONLINE. Kiểm tra và gắn session phải nguyên tử. Register kiểm tra unique username trong DB.

User search tìm chuỗi con trong Username/DisplayName, không phân biệt hoa/thường. Search/presence loại bản thân. Limit 1–50, AfterId là int hoặc null. Sắp tăng theo Id, lọc Id > AfterId; NextAfterId là ID cuối trang nếu còn kết quả, hết thì null. Trang rỗng có Items rỗng/cursor null. Không cam kết snapshot khi dữ liệu đổi giữa các trang.

Client giữ presence event trong lúc tải các trang online rồi áp dụng lại theo thứ tự sau khi dựng danh sách. Server cần xếp response/event cùng thứ tự đọc và thay đổi presence trên từng kết nối, tránh snapshot cũ đi sau event mới. V1 chưa có heartbeat ứng dụng; mất mạng âm thầm có thể chưa được phản ánh ngay.

### Bạn bè

| Tiền tố | Payload request | Data thành công |
|---|---|---|
| friend.list | {Status, Limit, AfterId} | {Items: FriendEntry[], NextAfterId} |
| friend.add | {UserId} | {Friendship} với Status Pending |
| friend.respond | {UserId, Accept: bool} | {Friendship}; null khi từ chối |
| friend.remove | {UserId} | {UserId} |

List lọc Pending/Accepted, gồm lời mời gửi lẫn nhận; Limit 1–50, phân trang tăng theo ID người còn lại. Trong respond, UserId là người gửi lời mời; chỉ người nhận được trả lời Pending. Chấp nhận chuyển Accepted, từ chối xóa lời mời. Remove chỉ xóa Accepted, không dùng để rút lời mời.

Không kết bạn chính mình. Mỗi cặp user có tối đa một quan hệ không phân biệt chiều: Pending cùng chiều trả FRIEND_REQUEST_EXISTS, ngược chiều trả INCOMING_REQUEST_EXISTS, đã Accepted trả ALREADY_FRIENDS. Kiểm tra phải an toàn khi có request đồng thời.

SQL dùng Friends.UserId1/UserId2 nhưng model dùng UserInvite/UserReceive. Khi triển khai cần chốt cách lưu chiều mời: hai ID đã sắp thứ tự của cặp không tự thể hiện ai mời ai. Không suy ra chiều mời khi dữ liệu chưa lưu thông tin đó.

### Tin nhắn và lịch sử

| Tiền tố | Payload request | Data thành công |
|---|---|---|
| chat.send | {ReceiverId, GroupId, Content} | {Message} |
| chat.history | {PeerId, GroupId, BeforeId, Limit} | {Items: Message[], NextBeforeId} |

Send có đúng một ReceiverId/GroupId khác null. History tương tự với PeerId/GroupId. Request gửi tin không có SenderId, ID hoặc thời gian.

Chat trực tiếp yêu cầu hai user khác nhau và là bạn Accepted; người nhận có thể offline. Server kiểm tra quyền, lưu thành công rồi trả response cho người gửi, đẩy event cho người nhận online. Không gửi thêm event cho người gửi. Đây là xác nhận đã lưu, không phải đã nhận/đã xem.

History trực tiếp chỉ trả tin của `(SenderId = currentUser AND ReceiverId = peer) OR (SenderId = peer AND ReceiverId = currentUser)`, đồng thời GroupId null. Xóa bạn vẫn được xem lịch sử chung nhưng không gửi mới. History nhóm chỉ dành cho thành viên hiện tại; họ được đọc cả tin trước lúc gia nhập.

History Limit 1–10. BeforeId null lấy các tin mới nhất; nếu có thì lọc Id < BeforeId. Chọn trang mới nhất phù hợp rồi trả Items tăng theo Id. NextBeforeId bằng ID nhỏ nhất trang vừa trả nếu còn tin cũ, hết thì null. Trang rỗng trả Items rỗng/cursor null. UI gộp theo Message.Id khi history và event giao nhau.

### Nhóm

| Tiền tố | Payload request | Data thành công |
|---|---|---|
| group.create | {GroupName, MemberIds: int[]} | {Group} |
| group.list | {Limit, AfterId} | {Items: Group[], NextAfterId} |
| group.members | {GroupId, Limit, AfterId} | {Items: User[], NextAfterId} |
| group.member.add | {GroupId, UserId} | {GroupId, UserId} |
| group.member.remove | {GroupId, UserId} | {GroupId, UserId} |

Create tự thêm chủ nhóm. MemberIds có thể rỗng, không trùng và không chứa chủ nhóm. Tối đa 20 thành viên tính cả chủ; người được thêm tồn tại và là bạn Accepted của chủ. Tạo nhóm/thành viên trong một transaction, lỗi thì không để nhóm dở.

Chỉ chủ nhóm quản lý thành viên. V1 chưa có chuyển chủ, xóa nhóm hoặc tự rời; không xóa chủ nhóm. Thêm người đã có trả ALREADY_MEMBER; xóa người không có trả NOT_MEMBER; vượt trần trả GROUP_FULL. Xóa bạn không tự xóa thành viên nhóm.

Group list chỉ trả nhóm của current user, tăng theo group Id. Group members chỉ thành viên được đọc, tăng theo user Id; cả hai dùng Limit 1–50/cursor như mục user. Người đã bị xóa không đọc/gửi tiếp; tin đã commit trước lúc xóa có thể đang trên đường truyền. Kiểm tra quyền và lưu tin/cập nhật membership phải nhất quán trong transaction hoặc khóa phù hợp.

SQL hiện có Groups nhưng chưa có bảng quan hệ thành viên. Cần bổ sung lưu membership ở task nhóm; schema JSON không thay thế database.

## 6. Danh mục event

| Type | Payload | Người nhận |
|---|---|---|
| presence.changed.event | {UserId, IsOnline} | Các kết nối xác thực khác khi user online/offline |
| friend.changed.event | {UserId, Friendship} | Hai bên online; UserId là người còn lại đối với từng người nhận, Friendship null khi xóa |
| chat.message.event | {Message} | Người nhận trực tiếp hoặc thành viên nhóm online khác người gửi |
| group.changed.event | {GroupId, Action, UserId, Group} | Theo quy tắc bên dưới |

Group Action gồm created/member_added/member_removed. Created có UserId null, gửi cho các thành viên online. Member_added có UserId là người được thêm, gửi cho các thành viên sau thay đổi. Member_removed có UserId là người bị xóa, gửi cho các thành viên còn lại và cả người bị xóa nếu online để họ bỏ nhóm khỏi UI. Group là object hiện hành, không phải danh sách thành viên.

Response chat.send, event chat.message và history chứa cùng bản Message đã lưu: cùng ID, tác giả, nội dung và UTC. UI so SenderId với user hiện tại để phân biệt tin mình/người khác. User offline đồng bộ list/history sau login; không hứa phát lại mọi event cũ.

## 7. Mã lỗi và vòng đời kết nối

Envelope đọc được an toàn, RequestId hợp lệ nhưng version/type chưa hỗ trợ: trả gói lỗi chung:

```json
{"Version":1,"Type":"system.error.response","RequestId":"4f083e5d-d921-4fd2-b15e-145cf2e7dc15","Payload":{"Code":"UNKNOWN_TYPE","Message":"Loại gói chưa được hỗ trợ."}}
```

Gói lỗi chung có Payload chính là Error, không bọc thêm Success/Data/Error. Sai payload của type đã biết trả response tương ứng với INVALID_REQUEST. Nếu không thể xác định envelope/RequestId an toàn thì đóng kết nối, không đoán ID từ JSON hỏng. Version số nguyên khác 1 trả UNSUPPORTED_VERSION qua envelope v1 rồi đóng. Version sai kiểu có thể trả INVALID_REQUEST nếu vẫn ghép được phản hồi. Client gửi response/event bị từ chối, không thực thi như request. Event chưa biết có thể được client bỏ qua; response sai cấu trúc cho request đang chờ là lỗi protocol.

| Code | Ý nghĩa |
|---|---|
| INVALID_REQUEST | Thiếu/sai trường, sai đích hoặc vi phạm giới hạn |
| UNSUPPORTED_VERSION / UNKNOWN_TYPE | Phiên bản/loại gói chưa hỗ trợ |
| NOT_AUTHENTICATED / ALREADY_AUTHENTICATED | Chưa đăng nhập hoặc kết nối đã xác thực |
| INVALID_CREDENTIALS | Sai username/password; không phân biệt user không có và password sai |
| USERNAME_TAKEN / ALREADY_ONLINE | Trùng username hoặc user đang có phiên khác |
| NOT_FOUND / FORBIDDEN | Không thấy user/quan hệ hoặc thiếu quyền; nhóm không có/không thuộc quyền đều trả FORBIDDEN |
| NOT_FRIENDS / ALREADY_FRIENDS | Chưa là bạn hoặc đã là bạn |
| FRIEND_REQUEST_EXISTS / INCOMING_REQUEST_EXISTS | Lời mời cùng chiều/ngược chiều đã có |
| ALREADY_MEMBER / NOT_MEMBER / GROUP_FULL | Trạng thái hoặc số thành viên không hợp lệ |
| INTERNAL_ERROR | Lỗi server/DB; không trả stack trace, SQL hay bí mật |

Timeout chờ response đề xuất là 10 giây. Timeout/mất mạng nghĩa là chưa xác định kết quả: server có thể đã commit nhưng response chưa tới. RequestId chỉ ghép phản hồi, không chống lặp; không tự retry mutation như chat.send. Reconnect phải login lại, tải list/history và gộp tin theo Message.Id; hai tin cùng nội dung chưa đủ để xác định cùng một lần gửi.

Logout ngừng nhận lệnh mới, xóa session, gửi response rồi đóng; cleanup offline chỉ một lần. EOF/lỗi socket cũng cleanup. Không log toàn bộ gói auth, Password hoặc PasswordHash. Bảo vệ bằng TLS và kiểm tra chứng chỉ trước khi dùng mật khẩu thật giữa các máy; demo TCP chưa TLS chỉ dùng tài khoản thử nghiệm. Không có claim exactly-once hoặc event delivery bền vững trong v1.

## 8. Triển khai và nghiệm thu

1. Giữ demo chat hiện tại, dùng mẫu chat để đối chiếu JSON/tiếng Việt.
2. Hoàn thiện framing có giới hạn và EOF, validation envelope và cơ chế ghép RequestId.
3. Thêm handler auth/session, friend/presence, chat trực tiếp/history rồi nhóm theo phân công.
4. Khi thêm type, cập nhật DTO, handler hai phía, đặc tả, schema và ví dụ trong cùng thay đổi.

Kiểm tra khi có code nghiệp vụ:

- Chia khung giữa ký tự UTF-8; gộp nhiều khung một lần đọc; EOF còn gói dở.
- Content có tiếng Việt, emoji, dấu nháy, newline; không serialize JSON hai lần.
- Version/type sai, payload sai kiểu, thiếu RequestId, vượt byte trần: không xử lý gói hỏng.
- Event xen response; giữ RequestId; response/event/history có cùng Message.Id.
- Gửi trước login, tự chèn SenderId, đọc chat người khác, gửi vào nhóm không thuộc quyền: bị từ chối.
- User offline đọc được tin đã lưu sau login; không tự gửi lại khi timeout.
- Hai login/lời mời đồng thời, xóa bạn/thành viên trong lúc gửi: quyền/dữ liệu nhất quán.
- History hơn 10 tin, có tin mới xen vào; cursor không làm trùng tin hiển thị.

Typing/seen, file/ảnh/video, tìm tin nhắn, token lưu phiên và mã hóa đầu cuối chưa thuộc bộ gói này. File nhị phân cần thiết kế riêng, không ghi trực tiếp vào luồng JSON theo dòng hoặc nhét nguyên file vượt trần vào Payload.
