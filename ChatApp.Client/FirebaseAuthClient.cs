using System;
using System.Text.Json;
using System.Net.Http.Json;

namespace ChatApp.Client
{
    /// <summary>
    /// Lớp hỗ trợ xác thực người dùng với Firebase.
    /// </summary>
    public sealed class FirebaseAuthClient
    {
        private static readonly HttpClient Http = new()
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
        private readonly string _apiKey;
        public FirebaseAuthClient(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("Chưa cấu hình FIREBASE_WEB_API_KEY.", nameof(apiKey));
            }
            _apiKey = apiKey;
        }
        private async Task<string> SendAsync(string action, string email, string password, CancellationToken ct = default)
        {
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:{action}?key={Uri.EscapeDataString(_apiKey)}";
            using var response = await Http.PostAsJsonAsync(url, new
            {
                email = email.Trim(),
                password,
                returnSecureToken = true
            }, ct);
            string body = await response.Content.ReadAsStringAsync(ct);
            using var json = JsonDocument.Parse(body);
            if(!response.IsSuccessStatusCode)
            {
                string code = "FIREBASE_ERROR";
                if(json.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("message", out var message))
                {
                    code = message.GetString() ?? code;
                }
                throw new FirebaseSignInException(code);
            }
            if(!json.RootElement.TryGetProperty("idToken", out var token) || string.IsNullOrWhiteSpace(token.GetString()))
            {
                throw new InvalidDataException("Firebase không trả về ID token.");
            }
            return token.GetString()!;
        }
        public Task<string> RegisterAsync(string email, string password, CancellationToken ct = default)
        {
            return SendAsync("signUp", email, password, ct);
        }
        public Task<string> LoginAsync(string email, string password, CancellationToken ct = default)
        {
            return SendAsync("signInWithPassword", email, password, ct);
        }
        public async Task SendVerificationEmailAsync(string idToken, CancellationToken ct = default)
        {
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={Uri.EscapeDataString(_apiKey)}";
            using var response = await Http.PostAsJsonAsync(url, new
            {
                requestType = "VERIFY_EMAIL",
                idToken
            }, ct);
            string body = await response.Content.ReadAsStringAsync(ct);
            if(!response.IsSuccessStatusCode)
            {
                using var json = JsonDocument.Parse(body);
                string code = "FIREBASE_ERROR";
                if(json.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("message", out var message))
                {
                    code = message.GetString() ?? code;
                }
                throw new FirebaseSignInException(code);
            }
        }
        public async Task<bool> IsEmailVerifiedAsync(string idToken, CancellationToken ct = default)
        {
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={Uri.EscapeDataString(_apiKey)}";

            using var response = await Http.PostAsJsonAsync(url, new 
            { 
                idToken 
            }, ct);

            string body = await response.Content.ReadAsStringAsync(ct);
            using var json = JsonDocument.Parse(body);

            if(!response.IsSuccessStatusCode)
            {
                string code = "FIREBASE_ERROR";
                if(json.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("message", out var message))
                {
                    code = message.GetString() ?? code;
                }
                throw new FirebaseSignInException(code);
            }
            
            if(!json.RootElement.TryGetProperty("users", out var users) || users.ValueKind != JsonValueKind.Array || users.GetArrayLength() != 1)
            {
                throw new InvalidDataException("Firebase không trả về thông tin tài khoản hợp lệ.");
            }
            return users[0].TryGetProperty("emailVerified", out var verified) && verified.ValueKind == JsonValueKind.True;
        }
    }
    public sealed class FirebaseSignInException : Exception
    {
        public string Code { get; }

        public FirebaseSignInException(string code) : base(code)
        {
            Code = code;
        }
    }
}
