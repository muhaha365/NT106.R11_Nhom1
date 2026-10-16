namespace ChatApp.Shared.Contracts.Auth;

// Bản khởi đầu dùng username/password. Các phương thức đăng nhập khác sẽ bổ sung sau.
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
