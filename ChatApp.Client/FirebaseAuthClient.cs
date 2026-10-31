using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using System.Diagnostics;

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
            if(string.IsNullOrWhiteSpace(apiKey)) 
                throw new ArgumentException("Chưa cấu hình FIREBASE_WEB_API_KEY.", nameof(apiKey));
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
            => SendAsync("signUp", email, password, ct);
        public Task<string> LoginAsync(string email, string password, CancellationToken ct = default)
            => SendAsync("signInWithPassword", email, password, ct);
    }
    public sealed class FirebaseSignInException(string code) : Exception(code)
    {
        public string Code { get; } = code;
    }
}
