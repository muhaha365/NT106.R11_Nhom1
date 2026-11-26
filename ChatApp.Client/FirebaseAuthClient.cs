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
        private async Task<string> PostAsync(string action, object payload, CancellationToken ct = default)
        {
            string url =
                $"https://identitytoolkit.googleapis.com/v1/accounts:{action}?key={Uri.EscapeDataString(_apiKey)}";

            using var response = await Http.PostAsJsonAsync(url, payload, ct);
            string body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                using var json = JsonDocument.Parse(body);
                string code = "FIREBASE_ERROR";

                if (json.RootElement.TryGetProperty("error", out var error)
                    && error.TryGetProperty("message", out var message))
                {
                    code = message.GetString() ?? code;
                }

                throw new FirebaseSignInException(code);
            }

            return body;
        }
        public async Task<string> RegisterAsync(string email, string password, CancellationToken ct = default)
        {
            string body = await PostAsync("signUp", new
            {
                email = email.Trim(),
                password,
                returnSecureToken = true
            }, ct);

            using var json = JsonDocument.Parse(body);

            if (!json.RootElement.TryGetProperty("idToken", out var token)
                || string.IsNullOrWhiteSpace(token.GetString()))
            {
                throw new InvalidDataException(
                    "Firebase không trả về ID token.");
            }
            return token.GetString()!;
        }
        public async Task<string> LoginAsync(string email, string password, CancellationToken ct = default)
        {
            string body = await PostAsync("signInWithPassword", new
            {
                email = email.Trim(),
                password,
                returnSecureToken = true
            }, ct);

            using var json = JsonDocument.Parse(body);

            if (!json.RootElement.TryGetProperty("idToken", out var token)
                || string.IsNullOrWhiteSpace(token.GetString()))
            {
                throw new InvalidDataException(
                    "Firebase không trả về ID token.");
            }
            return token.GetString()!;
        }
        public async Task SendVerificationEmailAsync(string idToken, CancellationToken ct = default)
        {
            await PostAsync("sendOobCode", new
            {
                requestType = "VERIFY_EMAIL",
                idToken
            }, ct);
        }
        public async Task<bool> IsEmailVerifiedAsync(string idToken, CancellationToken ct = default)
        {
            string body = await PostAsync("lookup", new
            {
                idToken
            }, ct);

            using var json = JsonDocument.Parse(body);

            if (!json.RootElement.TryGetProperty("users", out var users)
                || users.ValueKind != JsonValueKind.Array
                || users.GetArrayLength() != 1)
            {
                throw new InvalidDataException(
                    "Firebase không trả về thông tin tài khoản hợp lệ.");
            }

            return users[0].TryGetProperty("emailVerified", out var verified)
                && verified.ValueKind == JsonValueKind.True;
        }

        public async Task SendPasswordResetEmailAsync(string email, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Vui lòng nhập email.");
            }

            await PostAsync("sendOobCode", new
            {
                requestType = "PASSWORD_RESET",
                email = email.Trim()
            }, ct);
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
