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
        private DocumentReference User(int id) => _db.Collection("user").Document(Key(id));
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

        // private static UserSummary ReadUser(DocumentSnapshot doc) 
        // => new UserSummary(doc.GetValue<int>("Id"),
            // doc.GetValue<string>("Username"),
            // doc.GetValue<string>("DisplayName"), false);

        public async Task<UserSummary?> GetUserAsync(int id, CancellationToken ct = default)
        {
            if(id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            var doc = await User(id).GetSnapshotAsync(ct);
            if(!doc.Exists) return null;
            return new UserSummary(doc.GetValue<int>("Id"), doc.GetValue<string>("Username"), doc.GetValue<string>("DisplayName"), false);
        }
    }
}