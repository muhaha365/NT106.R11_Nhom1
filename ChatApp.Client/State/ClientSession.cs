using ChatApp.Shared.Contracts.Users;

namespace ChatApp.Client.State;

// Giữ trạng thái giao diện trong bộ nhớ. Chưa có chức năng lưu phiên xuống ổ đĩa.
public class ClientSession
{
    public UserSummaryDto? CurrentUser { get; set; }

    // Đây chỉ là trạng thái ở client; server vẫn phải tự kiểm tra quyền.
    public bool IsLoggedIn => CurrentUser is not null;
}
