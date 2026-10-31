# Đăng ký / đăng nhập Firebase cho mốc báo cáo giữa kỳ

Đối chiếu source sau merge `247d15a`, ngày 31/10/2026. Đây là hướng dẫn triển khai; các đoạn mẫu bên dưới chưa được ghép vào ứng dụng chính. Giữ cách truyền Packet hiện có và dùng lại ProtocolClient.

## 1. Mục tiêu và hiện trạng

Mốc đầu tiên: đăng ký bằng email/mật khẩu, tạo hồ sơ ứng dụng, đăng nhập đúng/sai, gắn danh tính vào kết nối TCP, mở MainForm với đúng UserSummary, đăng xuất và đăng nhập lại sau khi khởi động lại server. Google và số điện thoại triển khai sau mốc này nếu giảng viên không yêu cầu ở giữa kỳ.

| File hiện tại | Đã có | Cần thay/bổ sung |
|---|---|---|
| LoginForm.cs | Kiểm tra rỗng, kết nối TCP, gửi lời chào chat | Đăng nhập email với Firebase, gửi ID token đến server |
| RegisterForm.cs | Kiểm tra input, tính SHA256 nhưng chưa gửi đăng ký | Thêm Email, gọi Firebase tạo tài khoản rồi tạo hồ sơ qua server |
| ProtocolClient.cs | RequestId, vòng nhận, timeout, đọc Success/Data/Error | Dùng nguyên API RequestAsync; không thêm vòng ReceiveAsync trong form |
| Database.cs | Firestore, users/authUsers/usernames/counter | Tra user theo UID đã xác minh; transaction tạo hồ sơ |
| Program.cs server | Khởi tạo Firestore, kiểm tra đọc/ghi | Khởi tạo Firebase Admin một lần |
| ChatServer.cs | Nhận chat rồi broadcast | Handler auth, response, session và chặn người chưa đăng nhập |
| ConnectedClient.cs | Id kết nối demo | UserId nghiệp vụ, hạn dùng phiên |
| MainForm.cs | Constructor nhận ProtocolClient và UserSummary | Dùng constructor này; xử lý các chức năng chưa có handler |

`CheckConnectionAsync` thành công chỉ chứng minh đọc/ghi Firestore. Nó chưa kiểm tra Firebase Authentication. `EnsureProfileAsync` tạo hồ sơ trong Firestore, không tạo tài khoản email/mật khẩu.

## 2. Luồng cần hoàn thành

```mermaid
sequenceDiagram
    participant UI as WinForms
    participant Auth as Firebase Authentication
    participant S as ChatServer
    participant DB as Firestore
    UI->>Auth: HTTPS: email + mật khẩu (signUp hoặc signInWithPassword)
    Auth-->>UI: ID token
    UI->>S: auth.firebase.request: IdToken (+ hồ sơ khi đăng ký)
    S->>Auth: Admin SDK xác minh token, kiểm tra thu hồi/disabled
    Auth-->>S: Firebase UID đã xác minh
    S->>DB: Tìm mapping UID → UserId; tạo hồ sơ nếu cần
    DB-->>S: UserSummary
    Note over S: Gắn UserId vào đúng kết nối TCP
    S-->>UI: auth.firebase.response: Success + User
    Note over UI: Mở MainForm bằng kết nối vừa đăng nhập
```

Email dùng để đăng nhập Firebase. Username là tên công khai trong ứng dụng, ví dụ `longlong_01`. Không dùng username như email, không tự tạo email giả từ username.

Client gửi mật khẩu người dùng nhập nguyên vẹn đến HTTPS của Firebase. Bỏ HashSHA256 khỏi luồng này, không Trim mật khẩu, không lưu mật khẩu/hash trong Firestore. Hash phía client không thay thế mã hóa đường truyền.

Server dùng `verified.Uid` từ Admin SDK; không nhận `Uid`/`UserId` do client tự khai làm danh tính. ID token khác refresh token và custom token. [Xác minh ID token](https://firebase.google.com/docs/auth/admin/verify-id-tokens).

Hiện TCP dùng NetworkStream chưa mã hóa và chỉ bind loopback. Demo bước đầu dùng tài khoản thử nghiệm trên cùng máy. Trước khi đưa token qua LAN/Internet, ghép TLS bằng SslStream ở cả client/server và xác thực chứng chỉ server; không bỏ kiểm tra chứng chỉ. HTTPS tới Firebase không mã hóa chặng TCP này.

## 3. Cấu hình Firebase

1. Mở đúng project `chatapp-nhom1-41d53` trong Firebase Console.
2. Vào Authentication → Sign-in method, bật **Email/Password**, Save. Không cần bật email-link/passwordless cho luồng này. [Hướng dẫn bật provider](https://firebase.google.com/docs/auth/web/password-auth).
3. Trong Project settings → General, lấy **Web API Key**. Nếu chưa có ứng dụng/config tương ứng, đăng ký app Web để lấy trường `apiKey`; không cần tạo website hoặc bật Hosting. Kiểm tra API key thuộc cùng project với server. [API key Firebase](https://firebase.google.com/docs/projects/api-keys).
4. Client sử dụng API key; service-account JSON chỉ dùng trên server.

Chạy client từ cùng PowerShell vừa đặt biến:

```powershell
$env:FIREBASE_WEB_API_KEY = "DIEN_WEB_API_KEY_CUA_PROJECT"
dotnet run --project .\ChatApp.Client\ChatApp.Client.csproj
```

Server giữ hai biến đã cấu hình:

```powershell
$env:FIREBASE_PROJECT_ID = "chatapp-nhom1-41d53"
$env:GOOGLE_APPLICATION_CREDENTIALS = "C:\Users\longlong\.config\chatapp\firebase-service-account.json"
dotnet run --project .\ChatApp.Server\ChatApp.Server.csproj
```

`FIREBASE_WEB_API_KEY` là tên biến do hướng dẫn này chọn; code client bên dưới đọc nó. Nếu chạy F5, cần cung cấp biến cho tiến trình IDE/debug hoặc lưu cấp User rồi khởi động lại IDE. Không gửi nội dung service-account JSON cho client. API key cấu hình Firebase không phải private key quản trị.

## 4. Thống nhất bổ sung protocol trước khi ghép

Protocol hiện mô tả `auth.login` bằng Username/Password. Đề xuất **thêm** `auth.firebase.request/response`, giữ envelope Version/Type/RequestId/Payload. Không nhét token vào trường Password của giao thức cũ. Thành viên phụ trách protocol cập nhật docs/protocol.md, schema và examples theo bảng này; ProtocolClient hiện có đã gửi/đọc được tên request mới mà không cần viết lại.

| Tình huống | Payload của auth.firebase.request |
|---|---|
| Đăng nhập | `{IdToken, Username: null, DisplayName: null}` |
| Đăng ký / hoàn tất hồ sơ | `{IdToken, Username, DisplayName}` |

Server tìm hồ sơ bằng UID đã verify trước. Nếu đã có hồ sơ, trả hồ sơ đó, không dùng dữ liệu request để đổi tên. Nếu chưa có và thiếu Username/DisplayName, trả `PROFILE_REQUIRED`; chưa tạo session. Nếu chưa có và đủ thông tin hợp lệ, tạo hồ sơ trong transaction rồi gắn session.

Thêm file `ChatApp.Shared/Contracts/Authentication.cs`:

```csharp
// SAMPLE: Authentication.cs
namespace ChatApp.Shared.Contracts;

public sealed record FirebaseLoginRequest(
    string IdToken, string? Username, string? DisplayName);
public sealed record AuthenticationResult(UserSummary User);
public sealed record EmptyResult();
```

Ví dụ response thành công (token chỉ xuất hiện trong request, không trả lại):

```json
{
  "Version": 1,
  "Type": "auth.firebase.response",
  "RequestId": "guid-cua-request",
  "Payload": {
    "Success": true,
    "Data": { "User": { "Id": 1, "Username": "longlong_01", "DisplayName": "Long", "IsOnline": true } },
    "Error": null
  }
}
```

Response lỗi vẫn dùng `auth.firebase.response`, giữ RequestId, Payload là `{Success:false, Data:null, Error:{Code,Message}}`. Với system.error.response thì Payload trực tiếp là `{Code,Message}` theo ProtocolClient hiện có.

Giữ quy định hiện tại: một tài khoản chỉ có một kết nối đã xác thực. Bổ sung các mã lỗi chưa có trong schema từ handler, gồm `PROFILE_REQUIRED`, `INVALID_TOKEN`, `AUTH_UNAVAILABLE`, `USER_DISABLED` và `NOT_IMPLEMENTED`. Đăng xuất dùng `auth.logout.request` đã có trong thiết kế, thành công Data là `{}` rồi đóng kết nối.

## 5. Client gọi Firebase qua HTTPS

Thêm `ChatApp.Client/FirebaseAuthClient.cs`. Không cần cài FirebaseAdmin vào client; .NET hiện tại đã có HttpClient và System.Net.Http.Json.

```csharp
// SAMPLE: FirebaseAuthClient.cs
using System.Net.Http.Json;
using System.Text.Json;

namespace ChatApp.Client;

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
            throw new ArgumentException("Chưa cấu hình FIREBASE_WEB_API_KEY.");
        _apiKey = apiKey;
    }

    public Task<string> RegisterAsync(string email, string password,
        CancellationToken ct = default)
        => SendAsync("signUp", email, password, ct);

    public Task<string> LoginAsync(string email, string password,
        CancellationToken ct = default)
        => SendAsync("signInWithPassword", email, password, ct);

    private async Task<string> SendAsync(string action, string email,
        string password, CancellationToken ct)
    {
        string url = "https://identitytoolkit.googleapis.com/v1/accounts:"
            + action + "?key=" + Uri.EscapeDataString(_apiKey);
        using var response = await Http.PostAsJsonAsync(url, new
        {
            email = email.Trim(),
            password, // Giữ nguyên, không Trim và không hash.
            returnSecureToken = true
        }, ct);
        string body = await response.Content.ReadAsStringAsync(ct);
        using var json = JsonDocument.Parse(body);
        if (!response.IsSuccessStatusCode)
        {
            string code = "FIREBASE_ERROR";
            if (json.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var message))
                code = message.GetString() ?? code;
            throw new FirebaseSignInException(code);
        }

        if (!json.RootElement.TryGetProperty("idToken", out var token)
            || string.IsNullOrWhiteSpace(token.GetString()))
            throw new InvalidDataException("Firebase không trả về ID token.");
        return token.GetString()!;
    }
}

public sealed class FirebaseSignInException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
```

Hai REST endpoint chính là `accounts:signUp` và `accounts:signInWithPassword`, request có email/password/returnSecureToken. [Firebase Auth REST](https://firebase.google.com/docs/reference/rest/auth).

UI cần bắt riêng FirebaseSignInException, lỗi mạng HTTP, JSON sai định dạng và timeout. Không hiển thị/log toàn bộ response thành công vì có token. Đoạn mẫu lấy ID token cho phiên hiện tại; chưa triển khai “ghi nhớ đăng nhập” hoặc tự làm mới refresh token.

## 6. Server khởi tạo Admin SDK

Server đã tham chiếu FirebaseAdmin 3.7.0. Trong Program.cs thêm hai using:

```csharp
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
```

Ngay sau khi đọc projectId, trước khi Run server, thêm một lần:

```csharp
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.GetApplicationDefault(),
    ProjectId = projectId
});
```

FirestoreDb và FirebaseApp là hai đối tượng phục vụ hai công việc khác nhau. Tạo FirestoreDb không tự khởi tạo FirebaseAuth.DefaultInstance. [Khởi tạo Admin SDK](https://firebase.google.com/docs/admin/setup).

## 7. Database: tìm hồ sơ bằng UID và tạo hồ sơ nguyên tử

Thêm phương thức sau **bên trong Database**. Chỉ gọi với UID lấy từ token đã verify:

```csharp
// SAMPLE: Database.GetUserByFirebaseUidAsync
public async Task<UserSummary?> GetUserByFirebaseUidAsync(
    string verifiedUid, CancellationToken ct = default)
{
    var mappingRef = _db.Collection("authUsers")
        .Document(Uri.EscapeDataString(verifiedUid));
    var mapping = await mappingRef.GetSnapshotAsync(ct);
    if (!mapping.Exists) return null;

    int id = mapping.GetValue<int>("UserId");
    return await GetUserAsync(id, ct)
        ?? throw new InvalidDataException("Có mapping nhưng thiếu hồ sơ user.");
}
```

Thay **toàn bộ** EnsureProfileAsync bằng phiên bản dưới đây. Source hiện tại vẫn đọc/ghi counter bằng các lệnh riêng; cần transaction để hai đăng ký đồng thời không lấy cùng ID, và không chỉ lưu được một phần hồ sơ.

Mẫu dùng username 3–50 ký tự theo protocol hiện tại; đồng thời sửa validate form cho cùng quy tắc. Database đang dùng 3–20, còn form chỉ cho chữ/số: ba chỗ này phải thống nhất.

```csharp
// SAMPLE: Database.EnsureProfileAsync
public async Task<UserSummary> EnsureProfileAsync(string verifiedUid,
    string username, string displayName, CancellationToken ct = default)
{
    if (string.IsNullOrWhiteSpace(verifiedUid)
        || string.IsNullOrWhiteSpace(username)
        || string.IsNullOrWhiteSpace(displayName))
        throw new ArgumentException("INVALID_REQUEST");

    username = username.Trim().ToLowerInvariant();
    displayName = displayName.Trim();
    if (!Regex.IsMatch(username, "\\A[a-z0-9_]{3,50}\\z")
        || displayName.EnumerateRunes().Count() is < 1 or > 100
        || displayName.Any(char.IsControl))
        throw new ArgumentException("INVALID_REQUEST");

    var mappingRef = _db.Collection("authUsers")
        .Document(Uri.EscapeDataString(verifiedUid));
    var nameRef = _db.Collection("usernames").Document(username);
    var counterRef = _db.Collection("counters").Document("users");

    return await _db.RunTransactionAsync(async tx =>
    {
        var mapping = await tx.GetSnapshotAsync(mappingRef);
        if (mapping.Exists)
        {
            int id = mapping.GetValue<int>("UserId");
            var existing = await tx.GetSnapshotAsync(User(id));
            if (!existing.Exists)
                throw new InvalidDataException("Có mapping nhưng thiếu hồ sơ user.");
            return ReadUser(existing);
        }

        // Đọc tất cả dữ liệu cần kiểm tra trước khi ghi.
        var name = await tx.GetSnapshotAsync(nameRef);
        var counter = await tx.GetSnapshotAsync(counterRef);
        if (name.Exists) throw new InvalidOperationException("USERNAME_TAKEN");
        int idNew = checked((counter.Exists ? counter.GetValue<int>("LastId") : 0) + 1);

        tx.Set(counterRef, new Dictionary<string, object> { ["LastId"] = idNew });
        tx.Create(User(idNew), new Dictionary<string, object>
        {
            ["Id"] = idNew,
            ["FirebaseUid"] = verifiedUid,
            ["Username"] = username,
            ["DisplayName"] = displayName,
            ["CreatedAt"] = FieldValue.ServerTimestamp
        });
        tx.Create(mappingRef, new Dictionary<string, object> { ["UserId"] = idNew });
        tx.Create(nameRef, new Dictionary<string, object> { ["UserId"] = idNew });
        return new UserSummary(idNew, username, displayName, false);
    }, cancellationToken: ct);
}
```

Transaction có thể chạy lại callback. Vì vậy chỉ đọc/ghi database trong callback; gửi response sau khi await hoàn tất. [Transaction Firestore](https://firebase.google.com/docs/firestore/manage-data/transactions).

Transaction này chỉ bao phủ hồ sơ Firestore. Tạo tài khoản Firebase Authentication và tạo hồ sơ Firestore vẫn là hai bước riêng; xem phần phục hồi ở mục 10.

## 8. Session và handler xác thực trên server

Thêm vào ConnectedClient; giữ nguyên Id hiện tại để log kết nối:

```csharp
public int? UserId { get; set; }
public DateTimeOffset? SessionExpiresAt { get; set; }
```

Trong ChatServer thêm using FirebaseAdmin.Auth, ChatApp.Shared.Contracts và thêm các thành viên bên dưới. `_sessionLock` bảo vệ việc kiểm tra/gắn user online; nó không thay thế transaction Firestore. Mẫu dùng thời hạn token cho phiên TCP; hết hạn thì đăng nhập lại. Không coi token đã verify một lần là phiên vô thời hạn. `checkRevoked: true` kiểm tra thu hồi tại thời điểm đăng nhập; thêm GetUserAsync để kiểm tra Disabled rõ ràng với FirebaseAdmin .NET 3.7.0. Chưa có cơ chế đẩy lệnh thu hồi tức thì tới phiên đang mở. [Phiên Firebase](https://firebase.google.com/docs/auth/admin/manage-sessions).

```csharp
// SAMPLE: ChatServer.AuthenticationMembers
private readonly object _sessionLock = new();
private readonly Dictionary<int, ConnectedClient> _onlineUsers = new();

private static Task ReplyAsync(ConnectedClient client, Packet request,
    object? data, string? code, string? message, CancellationToken ct)
{
    var payload = new
    {
        Success = code is null,
        Data = code is null ? data : null,
        Error = code is null ? null : new { Code = code, Message = message }
    };
    return client.SendPacketAsync(new Packet
    {
        Type = request.Type[..^8] + ".response",
        RequestId = request.RequestId,
        Payload = JsonSerializer.SerializeToElement(payload)
    }, ct);
}

private void ReleaseSession(ConnectedClient client)
{
    lock (_sessionLock)
    {
        if (client.UserId is int id
            && _onlineUsers.TryGetValue(id, out var owner)
            && ReferenceEquals(owner, client))
            _onlineUsers.Remove(id);
        client.UserId = null;
        client.SessionExpiresAt = null;
    }
}

private async Task HandleFirebaseLoginAsync(ConnectedClient client,
    Packet packet, CancellationToken ct)
{
    AuthenticationResult? result = null;
    string? code = null;
    string? message = null;
    try
    {
        if (client.UserId is not null)
            throw new InvalidOperationException("ALREADY_AUTHENTICATED");

        if (packet.Payload is not JsonElement payload
            || payload.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("INVALID_REQUEST");
        var request = payload.Deserialize<FirebaseLoginRequest>();
        if (request is null || string.IsNullOrWhiteSpace(request.IdToken)
            || request.IdToken.Length > 16384)
            throw new ArgumentException("INVALID_REQUEST");

        var verified = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(
            request.IdToken, checkRevoked: true, cancellationToken: ct);
        var account = await FirebaseAuth.DefaultInstance.GetUserAsync(verified.Uid, ct);
        if (account.Disabled) throw new InvalidOperationException("USER_DISABLED");
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(verified.ExpirationTimeSeconds);
        var user = await _database.GetUserByFirebaseUidAsync(verified.Uid, ct);
        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(request.Username)
                || string.IsNullOrWhiteSpace(request.DisplayName))
                throw new InvalidOperationException("PROFILE_REQUIRED");
            user = await _database.EnsureProfileAsync(verified.Uid,
                request.Username, request.DisplayName, ct);
        }

        lock (_sessionLock)
        {
            if (DateTimeOffset.UtcNow >= expiresAt)
                throw new InvalidOperationException("INVALID_TOKEN");
            if (_onlineUsers.ContainsKey(user.Id))
                throw new InvalidOperationException("ALREADY_ONLINE");
            client.UserId = user.Id;
            client.SessionExpiresAt = expiresAt;
            _onlineUsers.Add(user.Id, client);
        }
        result = new AuthenticationResult(user with { IsOnline = true });
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
    catch (FirebaseAuthException ex)
    {
        bool rejected = ex.AuthErrorCode is AuthErrorCode.InvalidIdToken
            or AuthErrorCode.ExpiredIdToken or AuthErrorCode.RevokedIdToken
            or AuthErrorCode.UserNotFound;
        code = rejected ? "INVALID_TOKEN" : "AUTH_UNAVAILABLE";
        message = rejected ? "Phiên Firebase không hợp lệ. Hãy đăng nhập lại."
            : "Chưa thể xác minh tài khoản. Hãy thử lại sau.";
    }
    catch (JsonException) { code = "INVALID_REQUEST"; message = "Payload không hợp lệ."; }
    catch (ArgumentException) { code = "INVALID_REQUEST"; message = "Kiểm tra lại dữ liệu tài khoản."; }
    catch (InvalidOperationException ex) when (ex.Message is "PROFILE_REQUIRED"
        or "USERNAME_TAKEN" or "ALREADY_AUTHENTICATED" or "ALREADY_ONLINE" or "INVALID_TOKEN"
        or "USER_DISABLED")
    {
        code = ex.Message;
        message = code switch
        {
            "PROFILE_REQUIRED" => "Tài khoản đã có, cần hoàn tất hồ sơ ứng dụng.",
            "USERNAME_TAKEN" => "Tên người dùng đã được sử dụng.",
            "ALREADY_ONLINE" => "Tài khoản đang đăng nhập ở kết nối khác.",
            "INVALID_TOKEN" => "Phiên Firebase hết hạn. Hãy đăng nhập lại.",
            "USER_DISABLED" => "Tài khoản đã bị vô hiệu hóa.",
            _ => "Kết nối này đã đăng nhập."
        };
    }
    catch (Exception ex)
    {
        // Chỉ log loại lỗi; không log packet, token hoặc mật khẩu.
        Console.Error.WriteLine($"Auth thất bại: {ex.GetType().Name}");
        code = "INTERNAL_ERROR";
        message = "Server chưa hoàn tất xác thực. Hãy thử lại sau.";
    }

    // Gửi ngoài try: nếu gửi lỗi, vòng nhận ngoài phải kết thúc và ReleaseSession.
    await ReplyAsync(client, packet, result, code, message, ct);
}
```

Mẫu giữ nguyên phiên đang đăng nhập nếu nhận yêu cầu đăng nhập lần hai; trả ALREADY_AUTHENTICATED. Nó không tự đổi danh tính trên cùng kết nối.

### Ghép vào HandleClientAsync

Đặt các nhánh sau sau kiểm tra `packet == null`, **trước** đoạn hiện có `if (packet.Type != "chat")`. Code xử lý request khác phải trả lỗi để ProtocolClient không chờ tới timeout:

```csharp
// SAMPLE: ChatServer.DispatchInsideLoop
if (packet.Version != 1 || string.IsNullOrWhiteSpace(packet.Type))
    break;
bool isRequest = packet.Type.EndsWith(".request", StringComparison.Ordinal);
if (isRequest && !Guid.TryParse(packet.RequestId, out _))
    break;

if (sender.UserId is not null
    && (sender.SessionExpiresAt is null || DateTimeOffset.UtcNow >= sender.SessionExpiresAt.Value))
{
    if (isRequest)
        await ReplyAsync(sender, packet, null, "NOT_AUTHENTICATED", "Phiên hết hạn. Hãy đăng nhập lại.", ct);
    break;
}

if (packet.Type == "auth.firebase.request")
{
    await HandleFirebaseLoginAsync(sender, packet, ct);
    continue;
}
if (sender.UserId is null)
{
    if (isRequest)
        await ReplyAsync(sender, packet, null, "NOT_AUTHENTICATED", "Bạn cần đăng nhập.", ct);
    break; // Cũng chặn gói chat demo của kết nối chưa xác thực.
}
if (packet.Type == "auth.logout.request")
{
    ReleaseSession(sender);
    await ReplyAsync(sender, packet, new EmptyResult(), null, null, ct);
    break;
}
if (isRequest)
{
    await sender.SendPacketAsync(new Packet
    {
        Type = "system.error.response",
        RequestId = packet.RequestId,
        Payload = JsonSerializer.SerializeToElement(new
        {
            Code = "NOT_IMPLEMENTED",
            Message = "Chức năng này chưa triển khai ở mốc giữa kỳ."
        })
    }, ct);
    continue;
}
```

Trong `finally` của HandleClientAsync gọi `ReleaseSession(sender)` trước khi Dispose. Đồng thời đồng bộ `_clients`: dùng khóa khi Add/Remove/ToArray; khi dừng server dùng snapshot để Dispose ngoài khóa. Không giữ `lock` trong lúc await.

Nếu tiếp tục giữ broadcast demo, phải lọc người nhận đã xác thực và chưa hết hạn. Ví dụ thân GetConnectedClients dùng cùng khóa `_sessionLock`:

```csharp
lock (_sessionLock)
{
    return _clients.Where(client => client.UserId is not null
        && client.SessionExpiresAt is DateTimeOffset expiry
        && expiry > DateTimeOffset.UtcNow).ToArray();
}
```

Khi shutdown cần snapshot **toàn bộ** `_clients`, kể cả khách chưa đăng nhập; không dùng snapshot đã lọc trên. Lưu ý chat demo vẫn là broadcast, chưa phải chat riêng có lưu Firestore. Khi làm nghiệp vụ, lấy người gửi từ `sender.UserId.Value`, không dùng `sender.Id`.

## 9. Ghép form theo các control đang có

### LoginForm

- Đổi ô `txtUsername` thành `txtEmail` bằng Designer, label “Email”.
- Bỏ `_connection`, HashSHA256 và luồng gửi chat/ReceiveAsync trong login.
- Đọc email bằng Trim; mật khẩu bằng `txtPassword.Text` nguyên vẹn.
- Tạo ProtocolClient mới cho mỗi lần thử, chỉ chuyển quyền sở hữu sang MainForm sau khi đăng nhập server thành công.

Thân xử lý chính dưới đây đặt trong event btn_connect_Click sau validate. Trong form cần using ChatApp.Shared.Contracts. Đây là mẫu trình tự UI; bổ sung CancellationTokenSource gắn với vòng đời form và kiểm tra IsDisposed sau await như code hiện tại.

```csharp
ProtocolClient? protocol = new ProtocolClient();
btn_connect.Enabled = false;
try
{
    var firebase = new FirebaseAuthClient(
        Environment.GetEnvironmentVariable("FIREBASE_WEB_API_KEY") ?? "");
    string idToken = await firebase.LoginAsync(txtEmail.Text.Trim(), txtPassword.Text);
    await protocol.ConnectAsync("127.0.0.1", 5000);
    var result = await protocol.RequestAsync<AuthenticationResult>(
        "auth.firebase.request", new FirebaseLoginRequest(idToken, null, null));

    using var main = new MainForm(protocol, result.User);
    protocol = null; // MainForm sở hữu kết nối từ đây, DisposeConnection đã có sẵn.
    Hide();
    try { main.ShowDialog(); }
    finally { if (!IsDisposed) Show(); }
}
catch (ProtocolRequestException ex) when (ex.Code == "PROFILE_REQUIRED")
{
    MessageBox.Show(this,
        "Tài khoản Firebase đã có. Mở Đăng ký, chọn Hoàn tất hồ sơ và nhập lại email/mật khẩu.");
}
catch (FirebaseSignInException ex)
{
    MessageBox.Show(this, FirebaseErrorMessage(ex.Code));
}
catch (ProtocolRequestException ex)
{
    MessageBox.Show(this, ex.Message);
}
catch (ArgumentException ex)
{
    MessageBox.Show(this, ex.Message);
}
catch (Exception)
{
    MessageBox.Show(this, "Chưa đăng nhập được. Kiểm tra kết nối và thử lại.");
}
finally
{
    protocol?.Dispose();
    if (!IsDisposed) { btn_connect.Enabled = true; btn_connect.Text = "Sign In"; }
}
```

`FirebaseErrorMessage` là helper UI cần viết theo bảng mục 11, không phải API có sẵn. Có thể đặt trong một file dùng chung cho hai form:

```csharp
// SAMPLE: AuthMessages.cs
namespace ChatApp.Client;

public static class AuthMessages
{
    public static string FirebaseErrorMessage(string code)
    {
        if (code.StartsWith("WEAK_PASSWORD", StringComparison.Ordinal)
            || code.StartsWith("PASSWORD_DOES_NOT_MEET_REQUIREMENTS", StringComparison.Ordinal))
            return "Mật khẩu chưa đáp ứng chính sách của ứng dụng.";
        return code switch
        {
            "EMAIL_EXISTS" => "Email đã có tài khoản. Hãy đăng nhập hoặc hoàn tất hồ sơ.",
            "INVALID_LOGIN_CREDENTIALS" or "INVALID_PASSWORD" or "EMAIL_NOT_FOUND"
                => "Email hoặc mật khẩu không đúng.",
            "INVALID_EMAIL" => "Email không hợp lệ.",
            "USER_DISABLED" => "Tài khoản đã bị vô hiệu hóa.",
            "OPERATION_NOT_ALLOWED" => "Cần bật Email/Password trong Firebase Console.",
            "TOO_MANY_ATTEMPTS_TRY_LATER" => "Bạn thử quá nhiều lần. Vui lòng thử lại sau.",
            _ => "Firebase chưa xử lý được yêu cầu. Kiểm tra cấu hình và kết nối."
        };
    }
}
```

Thêm `using static ChatApp.Client.AuthMessages;` vào form để gọi helper trên. Khi người dùng đóng form trong lúc await, hủy thao tác và dispose kết nối chưa bàn giao, không mở MainForm trên form đã đóng.

Không dùng `using var protocol` bao quanh rồi mở MainForm bằng Show không chặn: kết nối sẽ bị dispose lúc event kết thúc. Mẫu ShowDialog bên trên giữ vòng đời rõ ràng. Không gọi Close LoginForm ngay khi mở MainForm vì Program đang Application.Run(new LoginForm()).

### RegisterForm

Giữ txtUsername/txtDisplayName/txtPassword/txtConfirmPassword, thêm `txtEmail` và checkbox `chkExistingAccount` có nhãn “Tôi đã có tài khoản Firebase — hoàn tất hồ sơ”. Checkbox này dùng cho việc phục hồi đăng ký dở dang, không bỏ qua xác thực mật khẩu.

Validate trước khi gọi mạng: đủ các trường, email hợp lệ, username theo cùng quy tắc Database, mật khẩu bằng nhau và phù hợp chính sách Firebase. Không Trim hai ô mật khẩu. Bỏ HashSHA256 và ClientConnection trực tiếp.

Trình tự chính trong button1_Click_1:

```csharp
var firebase = new FirebaseAuthClient(
    Environment.GetEnvironmentVariable("FIREBASE_WEB_API_KEY") ?? "");
using var protocol = new ProtocolClient();

// Chỉ gọi signUp khi người dùng thực sự muốn tạo tài khoản mới.
string idToken = chkExistingAccount.Checked
    ? await firebase.LoginAsync(txtEmail.Text.Trim(), txtPassword.Text)
    : await firebase.RegisterAsync(txtEmail.Text.Trim(), txtPassword.Text);

// Từ thời điểm đã nhận token, mọi lần thử tiếp phải đi đường đăng nhập.
chkExistingAccount.Checked = true;
await protocol.ConnectAsync("127.0.0.1", 5000);
var result = await protocol.RequestAsync<AuthenticationResult>(
    "auth.firebase.request",
    new FirebaseLoginRequest(idToken, txtUsername.Text.Trim(), txtDisplayName.Text.Trim()));

// Đóng phiên đăng ký để trở lại LoginForm; server giải phóng user trước khi trả lời.
await protocol.RequestAsync<EmptyResult>("auth.logout.request", new { });
MessageBox.Show(this, $"Đã hoàn tất hồ sơ {result.User.Username}. Bạn có thể đăng nhập.");
Close();
```

Bao quanh đoạn này bằng try/catch/finally như LoginForm. Luôn bật lại btnRegister trong finally nếu form còn sống. `label13_Click` của LoginForm đã có đoạn hiện lại LoginForm khi RegisterForm đóng nên dùng lại được.

Nếu auth.firebase đã thành công nhưng logout lỗi/timeout, hồ sơ vẫn đã hoàn tất: dispose kết nối và thông báo đúng trạng thái, không báo “tạo tài khoản thất bại”. Nếu TCP auth timeout, đóng kết nối thử đó trước lần thử sau để không giữ một phiên mà UI tưởng chưa đăng nhập.

### MainForm

Giữ `new MainForm(protocol, result.User)`. Không tạo một ProtocolClient khác và không ConnectAsync lần nữa sau khi server đã xác thực socket hiện tại.

MainForm.Shown hiện gọi RefreshSessionAsync, rồi tự gọi friend.list/group.list. Server chưa có handler các request này. Trong mốc auth, tạm giữ SetSession nhưng hoãn hai RefreshAsync đó; hiện “Bạn bè/nhóm đang phát triển”. Đừng giả trả danh sách rỗng rồi báo tính năng đã hoàn thành. Khi có handler thật thì bật lại.

Ẩn/khóa nút Demo chat ở LoginForm cho luồng báo cáo xác thực. Server vẫn phải chặn khách chưa đăng nhập độc lập với UI. Sau khi disconnect trong MainForm, yêu cầu quay lại LoginForm; kết nối TCP mới cần xác thực lại. Cách đơn giản cho mốc này là đóng MainForm khi đăng xuất để LoginForm hiện lại. Chờ auth.logout nếu còn kết nối, sau đó luôn Dispose trong finally; nếu socket đã mất thì đóng form trực tiếp.

## 10. Phục hồi đăng ký dở dang

Trường hợp cần chủ động kiểm thử:

```text
signUp Firebase thành công
    → tài khoản đã tồn tại trong Authentication
    → server tắt / username trùng / lỗi Firestore / TCP timeout
    → hồ sơ ứng dụng có thể chưa có hoặc đã lưu nhưng response bị mất
```

Không tự động gọi signUp lại sau timeout và không kết luận “chưa tạo tài khoản”. Cho người dùng đăng nhập bằng email/mật khẩu vừa tạo:

1. Nếu đã có hồ sơ: đăng nhập bình thường, giữ nguyên UserId.
2. Nếu nhận PROFILE_REQUIRED: mở RegisterForm ở chế độ hoàn tất hồ sơ, gọi signInWithPassword rồi gửi token + username/displayName.
3. Nếu USERNAME_TAKEN: chọn username khác, tiếp tục bằng tài khoản Firebase hiện có.
4. Nếu signUp timeout mà chưa nhận token: báo kết quả chưa xác định; thử đăng nhập trước khi quyết định tạo lại. EMAIL_EXISTS cũng dẫn về đăng nhập/hoàn tất hồ sơ, không dùng để bỏ kiểm tra mật khẩu.

Không cần tự động xóa tài khoản Firebase để “rollback”. Transaction Firestore và mapping UID giúp việc thử lại không tạo hai hồ sơ cho cùng người dùng.

## 11. Thông báo lỗi để demo rõ ràng

| Nguồn/mã | Hiển thị/hành động |
|---|---|
| EMAIL_EXISTS | Email đã có tài khoản; chuyển đăng nhập hoặc hoàn tất hồ sơ |
| INVALID_LOGIN_CREDENTIALS / INVALID_PASSWORD / EMAIL_NOT_FOUND | Email hoặc mật khẩu không đúng; dùng một thông báo chung |
| INVALID_EMAIL | Email không hợp lệ |
| WEAK_PASSWORD... / PASSWORD_DOES_NOT_MEET_REQUIREMENTS | Mật khẩu chưa đáp ứng chính sách; kiểm tra prefix/mã trả về |
| OPERATION_NOT_ALLOWED | Chưa bật Email/Password hoặc cấu hình provider chưa phù hợp |
| TOO_MANY_ATTEMPTS_TRY_LATER | Tạm dừng thử, không tự retry liên tục |
| USER_DISABLED | Tài khoản đã bị vô hiệu hóa |
| PROFILE_REQUIRED | Đăng nhập Firebase được, cần hoàn tất hồ sơ ứng dụng |
| USERNAME_TAKEN | Chọn username khác, giữ tài khoản email hiện có |
| INVALID_TOKEN | Token sai/hết hạn/thu hồi/disabled; đăng nhập Firebase lại |
| ALREADY_ONLINE | Đăng xuất/đóng kết nối trước ở thiết bị kia |
| AUTH_UNAVAILABLE / INTERNAL_ERROR | Dịch vụ chưa xử lý được, không kết luận mật khẩu sai |
| TCP/HTTP timeout | Trạng thái có thể chưa xác định; đọc/đăng nhập lại trước khi thử tạo mới |

Firebase có thể trả lỗi đăng nhập chung khi bật bảo vệ dò email; không phụ thuộc việc luôn có EMAIL_NOT_FOUND. [Email enumeration protection](https://cloud.google.com/identity-platform/docs/admin/email-enumeration-protection).

## 12. Kế hoạch làm trong tuần và kiểm tra báo cáo

| Mốc | Công việc | Bằng chứng hoàn thành |
|---|---|---|
| 1 | Bật provider, thêm Email, viết FirebaseAuthClient | Tạo tài khoản thử; sai mật khẩu bị từ chối |
| 2 | DTO, Admin init, mapping/transaction, handler/session | Token hợp lệ có UserId; token giả bị từ chối |
| 3 | Ghép form → ProtocolClient → MainForm, logout | MainForm hiển thị đúng tên; đăng xuất rồi đăng nhập lại |
| 4 | Phục hồi lỗi, kiểm thử hai client, sửa UX | Không kẹt EMAIL_EXISTS, không trùng ID, không khách trái phép |
| 5 | Chạy lại kịch bản, chụp bằng chứng, chuẩn bị slide | Demo lặp lại được sau restart server |

Các thành viên có thể chia công việc UI/HTTPS, server/database, protocol/kiểm thử rồi ghép theo DTO đã thống nhất. Đây là phân công đề xuất cho nhóm, không phải các tính năng đã thực hiện.

Các ca kiểm tra tối thiểu:

1. Đăng ký mới: Authentication có tài khoản; Firestore có users/authUsers/usernames đồng nhất; không có Password/PasswordHash.
2. Đăng nhập đúng: MainForm hiển thị đúng UserSummary từ server, không lấy tên tự nhập để giả thành công.
3. Sai mật khẩu: không mở MainForm, không gắn UserId.
4. Email đã có, username trùng, mật khẩu xác nhận sai: thông báo đúng và nút được bật lại.
5. Firebase account có nhưng hồ sơ thiếu: PROFILE_REQUIRED → hoàn tất hồ sơ → đăng nhập thành công.
6. Token giả/hết hạn/khác project: server từ chối; không tạo hồ sơ, không gắn session.
7. Gói chat/request nghiệp vụ trước login: server từ chối; client chưa đăng nhập không nhận broadcast.
8. Hai đăng ký đồng thời: khác UID nhận ID khác; cùng username chỉ một hồ sơ thắng.
9. Hai client đăng nhập cùng user: chỉ một phiên được chấp nhận theo protocol hiện tại.
10. Tắt mạng/server sau signUp hoặc sau khi lưu hồ sơ: thử lại qua signin, không thêm hồ sơ trùng.
11. Logout/đóng MainForm rồi login lại: phiên cũ được giải phóng; đăng nhập sau restart giữ nguyên UserId.
12. Hết hạn phiên: server ngừng cho phép thao tác; UI yêu cầu đăng nhập lại. Không chỉ kiểm tra hạn token lúc mở socket.

Gợi ý báo cáo 5 phút: kiến trúc và phân công (1 phút), demo đăng ký/đăng nhập/sai mật khẩu/đăng xuất (3 phút), bằng chứng dữ liệu và phần còn lại (1 phút). Chụp Firebase Authentication Users, các document mapping/profile và MainForm; che token, mật khẩu và private key.

Phát biểu chính xác: “Nhóm dùng Firebase Authentication xác thực email/mật khẩu; server kiểm tra ID token, ánh xạ sang UserId và quản lý phiên TCP. Firestore lưu hồ sơ.” Không gọi “đã xác minh email” nếu chưa có luồng gửi thư xác minh và kiểm tra email_verified. Không báo Google/SĐT/chat riêng/bạn bè/nhóm đã hoàn thành nếu mới có UI.

## 13. Phạm vi kiểm chứng của hướng dẫn

Source ứng dụng chính chưa được sửa bởi tài liệu này. Các mẫu cần được ghép, build và chạy các ca trên bằng project Firebase của nhóm. Build thành công không chứng minh provider, quyền service account, đường truyền, transaction đồng thời hoặc luồng UI đã chạy thành công.

Đã trích các mẫu DTO, FirebaseAuthClient, AuthMessages, phương thức Database, khởi tạo Admin, handler và nhánh dispatch vào project tạm dùng .NET 10, FirebaseAdmin 3.7.0, Google.Cloud.Firestore 4.6.0. Hai build server và phần client không chứa WinForms đều đạt **0 warning, 0 error**. Đây là kiểm chứng biên dịch các mẫu cốt lõi, không phải kết quả kiểm thử toàn bộ hướng dẫn đã tích hợp. Các đoạn event UI còn yêu cầu thêm control, gắn hủy thao tác theo vòng đời form và xử lý đăng xuất như mô tả; chưa kiểm thử bằng thao tác WinForms hoặc tài khoản Firebase thật.
