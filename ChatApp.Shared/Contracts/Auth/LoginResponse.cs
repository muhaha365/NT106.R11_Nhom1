using ChatApp.Shared.Contracts.Users;

namespace ChatApp.Shared.Contracts.Auth;

public class LoginResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public UserSummaryDto? User { get; set; }
}
