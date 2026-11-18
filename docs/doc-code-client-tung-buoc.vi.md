# Đọc code client theo thao tác người dùng

Cập nhật ngày 18/11/2026. Bắt đầu từ các hàm dưới đây; chưa cần đọc phần vẽ giao diện.

## 1. Bấm nút thì chạy hàm nào?

| Thao tác | Điểm bắt đầu | Các bước chính |
|---|---|---|
| Mở màn hình chính | `MainForm_Shown` | Gắn user vào hai panel, tải bạn bè và nhóm |
| Tìm người | `FriendsPanel.SearchButton_Click` | `SearchAsync`: kiểm tra chữ nhập → gửi request → tạo từng dòng kết quả |
| Kết bạn | `FriendsPanel.AddButton_Click` | `AddAsync`: lấy người đang chọn → gửi request → cập nhật danh sách |
| Chọn nhóm | `GroupChatPanel.GroupsList_SelectedIndexChanged` | `SelectGroup`: giữ bản nháp nhóm cũ → chọn nhóm mới → tải lịch sử và tên thành viên |
| Gửi tin nhóm | `GroupChatPanel.SendGroupButton_Click` | `SendAsync`: kiểm tra nội dung → gửi request → hiển thị tin được server xác nhận |
| Tạo nhóm | `CreateGroupDialog.CreateButton_Click` | `CreateAsync`: lấy tên và ID bạn đã chọn → gửi request → đóng dialog khi thành công |
| Thêm thành viên | `GroupMembersDialog.AddMemberButton_Click` | Lấy bạn trong ComboBox → `ChangeMemberAsync` → tải lại thành viên |
| Xóa thành viên | `GroupMembersDialog.RemoveMemberButton_Click` | Lấy người trong ListView → hỏi xác nhận → `ChangeMemberAsync` |

**Giới hạn hiện tại:** `ChatServer` mới xử lý đăng nhập Firebase, đăng xuất và chat demo broadcast. Các request bạn bè/nhóm/lịch sử vẫn nhận `NOT_IMPLEMENTED`. Luồng UI đã có nhưng chưa được nối hoàn chỉnh với server. Chat demo trong `MainForm` không phải luồng lưu tin nhóm vào database.

## 2. Request đi và response về

Ví dụ `SearchAsync` gọi:

```csharp
var page = await _client.RequestAsync<PageResult<UserSummary>>(
    "user.search.request", new { Query = query, Limit = 50, AfterId = after }, token);
```

- `PageResult<UserSummary>` là kiểu dữ liệu mong nhận về: một trang người dùng.
- `new { ... }` gom các trường thành đối tượng để chuyển sang JSON.
- `RequestAsync` tạo `RequestId`, lưu vào `_pending`, rồi gửi qua `ClientConnection` → `PacketIO`.
- `await` chờ phản hồi mà không chặn giao diện. Người dùng vẫn có thể đóng cửa sổ.
- `ReceiveLoopAsync` là vòng nhận duy nhất. `DispatchPacket` tra `RequestId`, gọi `TrySetResult` để đánh thức đúng request.
- `ReadReply` đọc kết quả. Thành công thì trả dữ liệu cho `page`; server báo lỗi thì ném `ProtocolRequestException` về `catch` của nơi gọi.

`TaskCompletionSource` là chỗ giữ kết quả cho một tác vụ đang chờ. Cần nó vì cùng một kết nối vừa nhận response, vừa nhận event; không thể để mỗi nút tự đọc stream riêng.

## 3. Các lỗi có thể xảy ra lúc nào?

`throw` dừng hàm hiện tại và chuyển lỗi tới `catch` phù hợp, kể cả khi phải quay về hàm gọi qua `await`. `finally` vẫn chạy để mở lại nút hoặc dọn tài nguyên.

| Tình huống | Nơi xử lý | Điều người dùng thấy |
|---|---|---|
| Tên nhóm rỗng, tin quá dài | `if` tại form/panel, rồi `return` | Nhắc sửa dữ liệu; chưa gửi request, không cần exception |
| Chưa bật server mà bấm kết nối | Lỗi socket → `catch` của form | Không kết nối được |
| Tắt server khi client đang chờ | Vòng nhận đóng kết nối và kết thúc các request đang chờ | Mất kết nối; không chờ vô hạn |
| Server giữ kết nối nhưng không trả lời quá thời hạn (mặc định 10 giây) | `RequestAsync` ném `TimeoutException` | Yêu cầu tải lại để biết thao tác có thành công không |
| Bấm chức năng nhóm/bạn bè chưa có handler | Server trả `NOT_IMPLEMENTED` → `ProtocolRequestException` | “Chức năng này chưa phát triển” |
| Đóng dialog lúc đang tải | Hủy token → `OperationCanceledException` | Ngừng tải; không hiện lỗi vì người dùng đã đóng cửa sổ |
| Server trả sai JSON hoặc trang bị lặp | Kiểm tra protocol/phân trang | Báo dữ liệu không hợp lệ; tùy nơi phát hiện mà đóng kết nối hoặc dừng tải |

Không phải mọi `throw` đều có cách tái hiện bằng bấm nút. Ví dụ truyền sai tên loại request, gọi đối tượng đã `Dispose`, hoặc server trả sai JSON là lỗi lập trình/giao tiếp. Kiểm tra đó giúp phát hiện lỗi thay vì để chương trình dùng dữ liệu sai.

Nếu gửi tin bị timeout, **không thể kết luận server chưa lưu tin**: phản hồi có thể bị mất sau khi lưu. Vì thế `_sendNeedsRefresh` yêu cầu đồng bộ lịch sử trước khi gửi tiếp.

## 4. Những biến dễ gây khó hiểu

| Tên | Cách hiểu |
|---|---|
| `ListViewItem.Tag` | Đối tượng nằm sau dòng chữ: người dùng, quan hệ bạn bè hoặc nhóm |
| `_busy`, `_sending`, `_searching` | Đang làm việc này; khóa nút để tránh bấm lặp |
| `_sessionVersion` | Số phiên. Đổi phiên thì tăng số; kết quả của phiên cũ bị bỏ qua |
| `_selectionVersion` | Tương tự, nhưng cho nhóm đang chọn; tránh tin nhóm A hiện vào nhóm B |
| `CancellationToken` | Tín hiệu yêu cầu ngừng chờ, thường do đóng cửa sổ hoặc đổi phiên |
| `AfterId` / `NextAfterId` | Đọc trang sau ID này; ID phải tăng qua từng trang danh sách |
| `BeforeId` / `NextBeforeId` | Đọc tin cũ hơn ID này; ID phải giảm qua từng trang lịch sử |
| `ConcurrentDictionary` | Dictionary được truy cập an toàn từ nơi gửi và vòng nhận chạy nền |
| `SemaphoreSlim` | Chỉ cho một việc đi qua một thời điểm; dùng để tránh mở/ghi kết nối chồng nhau |
| `Interlocked.CompareExchange` | Trong một thao tác, kiểm tra kết nối còn là phiên cần đóng rồi mới đặt nó thành null |
| `OnUi` / `BeginInvoke` | Chuyển việc cập nhật control về luồng giao diện khi packet đến từ luồng nền |

`UiTheme` chỉ tạo/vẽ nút, danh sách và màu sắc. `MessageTimeline` chỉ sắp xếp khung tin, đo chữ và cuộn. `ScalePixels` đổi kích thước theo mức phóng to màn hình Windows. Các phần này không quyết định gửi mạng hay lưu database.

Để tự lần luồng, đặt breakpoint ở handler của nút → hàm xử lý → `RequestAsync` → `DispatchPacket` → dòng sau `await`. Với chức năng chưa nối server, đặt thêm breakpoint ở `catch` để thấy rõ `NOT_IMPLEMENTED` đi về UI.
