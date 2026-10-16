namespace ChatApp.Shared.Contracts.Users;

// Chỉ chứa thông tin được phép trả về client, không chứa password hash.
public class UserSummaryDto
{
    // ID trên đường truyền dùng chuỗi. Server ánh xạ với kiểu khóa của database.
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
