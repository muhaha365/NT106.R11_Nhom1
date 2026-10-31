using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ChatApp.Shared.Contracts;
using Google.Cloud.Firestore;


namespace ChatApp.Server
{
    public class Database
    {
        private readonly FirestoreDb _db;
        public Database(string projectId)
        {
            if(string.IsNullOrWhiteSpace(projectId))
            {
                throw new ArgumentException("Thiếu firebase ProjectId");
            }
            _db = FirestoreDb.Create(projectId);
        }
        private static string Key(int id) => id.ToString();
        private static string Pair(int a, int b) => a < b ? $"{Key(a)}_{Key(b)}" : $"{Key(b)}_{Key(a)}";
        private DocumentReference User(int id) => _db.Collection("users").Document(Key(id));
        // Chạy ở mốc kiểm tra kết nối, không chạy cho mỗi tin nhắn.
        public async Task CheckConnectionAsync(CancellationToken ct = default)
        {
            var doc = _db.Collection("diagnostics").Document("server-connection");
            await doc.SetAsync(new Dictionary<string, object>
            {
                ["CheckedAt"] = FieldValue.ServerTimestamp,
                ["Application"] = "ChatApp.Server"
            }, cancellationToken : ct);
            var result = await doc.GetSnapshotAsync(ct);
            if(!result.Exists)
            {
                throw new IOException("Không thể kết nối đến Firestore");
            }
        }

        private static UserSummary ReadUser(DocumentSnapshot doc) 
        => new UserSummary(doc.GetValue<int>("Id"),
            doc.GetValue<string>("Username"),
            doc.GetValue<string>("DisplayName"), false);

        public async Task<UserSummary?> GetUserAsync(int id, CancellationToken ct = default)
        {
            if(id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            var doc = await User(id).GetSnapshotAsync(ct);
            if(!doc.Exists) return null;
            return ReadUser(doc);
        }
        public async Task<UserSummary> EnsureProfileAsync(string verifiedUid, string username, string displayName, CancellationToken ct = default)
        {
            username = username.Trim().ToLowerInvariant();
            displayName = displayName.Trim();
            if(string.IsNullOrWhiteSpace(verifiedUid)
                || !Regex.IsMatch(username, "^[a-zA-Z0-9_]{3,20}$")
                || displayName.EnumerateRunes().Count() is < 1 or > 100
                || displayName.Any(char.IsControl))
            {
                throw new ArgumentException("INVALID_REQUEST");
            }
            var mappingref = _db.Collection("authUsers").Document(Uri.EscapeDataString(verifiedUid));
            var nameref = _db.Collection("usernames").Document(username);
            var counterref = _db.Collection("counters").Document("users");

            var mapping = await mappingref.GetSnapshotAsync(ct);
            if(mapping.Exists)
            {
                int existingId = mapping.GetValue<int>("UserId");
                var existingUser = await GetUserAsync(existingId, ct);
                return existingUser ?? throw new InvalidDataException("User mapping exists but user profile not found");
            }
            var name = await nameref.GetSnapshotAsync(ct);
            if(name.Exists)
            {
                throw new ArgumentException("USERNAME_TAKEN");
            }
            var counter = await counterref.GetSnapshotAsync(ct);
            int newId = (counter.Exists ? counter.GetValue<int>("LastId") : 0) + 1;
            await counterref.SetAsync(new Dictionary<string, object>
            {
                ["LastId"] = newId
            }, cancellationToken: ct);
            var doc = User(newId);
            await doc.SetAsync(new Dictionary<string, object>
            {
                ["Id"] = newId,
                ["FirebaseUid"] = verifiedUid,
                ["Username"] = username,
                ["DisplayName"] = displayName,
                ["CreatedAt"] = FieldValue.ServerTimestamp
            }, cancellationToken: ct);
            await mappingref.SetAsync(new Dictionary<string, object>
            {
                ["UserId"] = newId
            }, cancellationToken: ct);
            await nameref.SetAsync(new Dictionary<string, object>
            {
                ["UserId"] = newId
            }, cancellationToken: ct);
            return new UserSummary(newId, username, displayName, false);
        }
    }
}