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
        private static string Key(int id)
        {
            return id.ToString();
        }

        private static string Pair(int a, int b)
        {
            if (a < b)
            {
                return $"{Key(a)}_{Key(b)}";
            }
            return $"{Key(b)}_{Key(a)}";
        }

        private DocumentReference User(int id)
        {
            return _db.Collection("users").Document(Key(id));
        }
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
        {
            int id = doc.GetValue<int>("Id");
            string username = doc.GetValue<string>("Username");
            string displayName = doc.GetValue<string>("DisplayName");
            return new UserSummary(id, username, displayName, false);
        }

        public async Task<UserSummary?> GetUserAsync(int id, CancellationToken ct = default)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            var doc = await User(id).GetSnapshotAsync(ct);
            if (!doc.Exists)
            {
                return null;
            }
            return ReadUser(doc);
        }
        public async Task<UserSummary> EnsureProfileAsync(string verifiedUid, string username, string displayName, CancellationToken ct = default)
        {
            username = username.Trim().ToLowerInvariant();
            displayName = displayName.Trim();
            int displayNameLength = displayName.EnumerateRunes().Count();
            bool hasControlCharacter = false;
            foreach (char character in displayName)
            {
                if (char.IsControl(character))
                {
                    hasControlCharacter = true;
                    break;
                }
            }
            if(string.IsNullOrWhiteSpace(verifiedUid)
                || !Regex.IsMatch(username, @"\A[a-zA-Z0-9_]{3,20}\z")
                || displayNameLength < 1 || displayNameLength > 100
                || hasControlCharacter)
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
                int newId = 1;
                if (counter.Exists)
                {
                    newId = counter.GetValue<int>("LastId") + 1;
                }
                
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
            // Mỗi tin nhắn chỉ có một nơi nhận: một người hoặc một nhóm.
            bool hasReceiver = receiverId.HasValue;
            bool hasGroup = groupId.HasValue;
            if (me <= 0 || hasReceiver == hasGroup
                || (receiverId.HasValue && receiverId.Value <= 0)
                || (groupId.HasValue && groupId.Value <= 0)
                || string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("INVALID_REQUEST");
            }
            if (content.EnumerateRunes().Count() > 2000
                || Encoding.UTF8.GetByteCount(content) > 4000)
            {
                throw new InvalidOperationException("INVALID_REQUEST");
            }
            foreach (char character in content)
            {
                bool allowedWhitespace = character == '\t' || character == '\r' || character == '\n';
                if (char.IsControl(character) && !allowedWhitespace)
                {
                    throw new InvalidOperationException("INVALID_REQUEST");
                }
            }
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
                if (receiverId.HasValue)
                {
                    int peer = receiverId.Value;
                    var other = await tx.GetSnapshotAsync(User(peer));
                    var friendship = await tx.GetSnapshotAsync(_db.Collection("friendship").Document(Pair(me, peer)));
                    if(!other.Exists)
                    {
                        throw new InvalidOperationException("NOT_FOUND");
                    }
                    if(me == peer || !friendship.Exists
                        || friendship.GetValue<string>("Status") != "Accepted")
                    {
                        throw new InvalidOperationException("NOT_FRIENDS");
                    }
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
                    List<int> recipientIds = new List<int>();
                    foreach (int memberId in members)
                    {
                        if (memberId != me && !recipientIds.Contains(memberId))
                        {
                            recipientIds.Add(memberId);
                        }
                    }
                    recipients = recipientIds.ToArray();
                    conversation = $"group_{Key(groupId!.Value)}";
                }
                var counter = await tx.GetSnapshotAsync(counterref);
                int newId = 1;
                if (counter.Exists)
                {
                    newId = counter.GetValue<int>("LastId") + 1;
                }
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
        {
            int id = doc.GetValue<int>("Id");
            int senderId = doc.GetValue<int>("SenderId");
            int? receiverId = doc.GetValue<int?>("ReceiverId");
            int? groupId = doc.GetValue<int?>("GroupId");
            string content = doc.GetValue<string>("Content");
            DateTime sendTime = doc.GetValue<Timestamp>("SendTime").ToDateTime();
            return new MessageSummary(id, senderId, receiverId, groupId, content, new DateTimeOffset(sendTime));
        }
        
        public async Task<HistoryResult> GetHistoryAsync(int me, int? peerId, int? groupId, int? beforeId, int limit = 10, CancellationToken ct = default)
        {
            if(me <=0 || peerId.HasValue == groupId.HasValue
                || (peerId.HasValue && peerId.Value <= 0)
                || (groupId.HasValue && groupId.Value <= 0) || peerId == me
                || (beforeId.HasValue && beforeId.Value <= 0) || limit < 1 || limit > 100)
            {
                throw new InvalidOperationException("INVALID_REQUEST");
            }
            var current = await User(me).GetSnapshotAsync(ct);
            if(!current.Exists)
            {
                throw new InvalidOperationException("NOT_AUTHENTICATED");
            }
            string conversation;
            if (peerId.HasValue)
            {
                int peer = peerId.Value;
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
            if (beforeId.HasValue)
            {
                query = query.WhereLessThan("Id", beforeId.Value);
            }
            var snapshot = await query.Limit(limit + 1).GetSnapshotAsync(ct);
            List<MessageSummary> items = new List<MessageSummary>();
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                if (items.Count == limit)
                {
                    break;
                }
                items.Add(ReadMessage(document));
            }
            // Firestore trả tin mới nhất trước; giao diện hiển thị từ cũ đến mới.
            items.Reverse();
            int? next = null;
            if (snapshot.Count > limit)
            {
                next = items[0].Id;
            }
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
            UserSummary? user = await GetUserAsync(id, ct);
            if (user == null)
            {
                throw new InvalidDataException("Có mapping nhưng thiếu hồ sơ user.");
            }
            return user;
        }
    }
}
