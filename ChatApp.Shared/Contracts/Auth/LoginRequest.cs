namespace ChatApp.Shared.Contracts.Auth;

// Dữ liệu gửi tới server để đăng nhập. Không ghi Password vào log.
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
