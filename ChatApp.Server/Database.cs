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
            return await _db.RunTransactionAsync(async tx =>
            {
                var mapping = await tx.GetSnapshotAsync(mappingref);
                if(mapping.Exists)
                {
                    var existing = await tx.GetSnapshotAsync(User(mapping.GetValue<int>("UserId")));
                    if(!existing.Exists)
                    {
                        throw new InvalidDataException("Hồ sơ không nhất quán");
                    }
                    return ReadUser(existing);
                }
                // Đọc tất cả dữ liệu cần kiểm tra trước khi ghi.
                var name = await tx.GetSnapshotAsync(nameref);
                if(name.Exists)
                {
                    throw new InvalidOperationException("USERNAME_TAKEN");
                }
                
                var counter = await tx.GetSnapshotAsync(counterref);
                int newId = (counter.Exists ? counter.GetValue<int>("LastId") : 0) + 1;
                
                tx.Set(counterref, new Dictionary<string, object>
                {
                    ["LastId"] = newId
                });
                tx.Create(User(newId), new Dictionary<string, object>
                {
                    ["Id"] = newId,
                    ["FirebaseUid"] = verifiedUid,
                    ["Username"] = username,
                    ["DisplayName"] = displayName,
                    ["CreatedAt"] = FieldValue.ServerTimestamp
                });
                tx.Create(mappingref, new Dictionary<string, object>
                {
                    ["UserId"] = newId
                });
                tx.Create(nameref, new Dictionary<string, object>
                {
                    ["UserId"] = newId
                });
                return new UserSummary(newId, username, displayName, false);
            }, cancellationToken: ct);
        }
        public async Task<(MessageSummary Message, int[] recipients)> SendMessageAsync(int me, int? receiverId, int? groupId, string content, CancellationToken ct = default)
        {
            if (me <= 0 || receiverId.HasValue == groupId.HasValue
                || receiverId is <= 0 || groupId is <= 0
                || string.IsNullOrWhiteSpace(content)
                || content.EnumerateRunes().Count() > 2000
                || Encoding.UTF8.GetByteCount(content) > 4000
                || content.Any(c => char.IsControl(c) && c is not ('\t' or '\r' or '\n')))
                throw new InvalidOperationException("INVALID_REQUEST");
            var counterref = _db.Collection("counters").Document("messages");

            return await _db.RunTransactionAsync(async tx =>
            {
                var sender = await tx.GetSnapshotAsync(User(me));
                if(!sender.Exists)
                {
                    throw new InvalidOperationException("NOT_AUTHENTICATED");
                }
                int[] recipients;
                string conversation;
                if(receiverId is int peer)
                {
                    var other = await tx.GetSnapshotAsync(User(peer));
                    var firendship = await tx.GetSnapshotAsync(_db.Collection("friendship").Document(Pair(me, peer)));
                    if(!other.Exists)
                    {
                        throw new InvalidOperationException("NOT_FOUND");
                    }
                    if(me == peer || !firendship.Exists
                        || firendship.GetValue<string>("Status") != "Accepted")
                        throw new InvalidOperationException("NOT_FRIENDS");
                    recipients = new [] {peer};
                    conversation = $"direct_{Pair(me, peer)}";
                }
                else
                {
                    var group = await tx.GetSnapshotAsync(_db.Collection("groups").Document(Key(groupId!.Value)));
                    if(!group.Exists)
                    {
                        throw new InvalidOperationException("FORBIDDEN");
                    }
                    var members = group.GetValue<int[]>("MemberIds");
                    if(!members.Contains(me))
                    {
                        throw new InvalidOperationException("FORBIDDEN");
                    }
                    recipients = members.Where(id => id != me).Distinct().ToArray();
                    conversation = $"group_{Key(groupId!.Value)}";
                }
                var counter = await tx.GetSnapshotAsync(counterref);
                int newId = (counter.Exists ? counter.GetValue<int>("LastId") : 0 ) + 1;
                DateTime utc = DateTime.UtcNow;
                var message = new MessageSummary(newId, me, receiverId, groupId, content, new DateTimeOffset(utc));
                tx.Set(counterref, new Dictionary<string, object>
                {
                    ["LastId"] = newId
                });
                tx.Create(_db.Collection("messages").Document(Key(newId)), new Dictionary<string, object?>
                {
                    ["Id"] = newId,
                    ["ConversationKey"] = conversation,
                    ["SenderId"] = me,
                    ["ReceiverId"] = receiverId,
                    ["GroupId"] = groupId,
                    ["Content"] = content,
                    ["SendTime"] = Timestamp.FromDateTime(utc)
                });
                return (message, recipients);
            }, cancellationToken: ct);
        }

        private static MessageSummary ReadMessage(DocumentSnapshot doc)
        => new MessageSummary(doc.GetValue<int>("Id"),
            doc.GetValue<int>("SenderId"),
            doc.GetValue<int?>("ReceiverId"),
            doc.GetValue<int?>("GroupId"),
            doc.GetValue<string>("Content"),
            new DateTimeOffset(doc.GetValue<Timestamp>("SendTime").ToDateTime()));
        
        public async Task<HistoryResult> GetHistoryAsync(int me, int? peerId, int? groupId, int? beforeId, int limit = 10, CancellationToken ct = default)
        {
            if(me <=0 || peerId.HasValue == groupId.HasValue
                || peerId is <= 0 || groupId is <= 0 || peerId == me
                || beforeId is <=0 || limit is < 1 or > 100)
                throw new InvalidOperationException("INVALID_REQUEST");
            var current = await User(me).GetSnapshotAsync(ct);
            if(!current.Exists)
            {
                throw new InvalidOperationException("NOT_AUTHENTICATED");
            }
            string conversation;
            if(peerId is int peer)
            {
                var other = await User(peer).GetSnapshotAsync(ct);
                if(!other.Exists)
                {
                    throw new InvalidOperationException("NOT_FOUND");
                }
                conversation = $"direct_{Pair(me, peer)}";
            }
            else
            {
                var group = await _db.Collection("groups").Document(Key(groupId!.Value)).GetSnapshotAsync(ct);
                if(!group.Exists || !group.GetValue<int[]>("MemberIds").Contains(me))
                {
                    throw new InvalidOperationException("FORBIDDEN");
                }
                conversation = $"group_{Key(groupId!.Value)}";
            }
            var query = _db.Collection("messages")
                .WhereEqualTo("ConversationKey", conversation)
                .OrderByDescending("Id");
            if(beforeId is int before)
            {
                query = query.WhereLessThan("Id", before);
            }
            var snapshot = await query.Limit(limit + 1).GetSnapshotAsync(ct);
            var items = snapshot.Documents.Take(limit).Select(ReadMessage).OrderBy(m => m.Id).ToArray();
            int? next = snapshot.Count() > limit ? items[0].Id : null;
            return new HistoryResult(items, next);
        }
        public async Task<UserSummary?> GetUserByFirebaseUidAsync(string verifiedUid, CancellationToken ct = default)
        {
            var mappingref = _db.Collection("authUsers").Document(Uri.EscapeDataString(verifiedUid));
            var mapping = await mappingref.GetSnapshotAsync(ct);
            if(!mapping.Exists)
            {
                return null;
            }
            int id = mapping.GetValue<int>("UserId");
            return await GetUserAsync(id, ct) ?? throw new InvalidDataException("Có mapping nhưng thiếu hồ sơ user.");
        }
    }
}