namespace ChatApp.Shared.Contracts;

// Dữ liệu công khai trên socket, không dùng UserData có PasswordHash.
public sealed record UserSummary(int Id, string Username, string DisplayName, bool IsOnline);
public sealed record FriendshipSummary(int UserInvite, int UserReceive, string Status, DateTimeOffset CreatedAt);
public sealed record FriendEntry(UserSummary User, FriendshipSummary Friendship);
public sealed record GroupSummary(int Id, string GroupName, int CreatedBy, DateTimeOffset CreatedAt);
public sealed record MessageSummary(int Id, int SenderId, int? ReceiverId, int? GroupId, string Content, DateTimeOffset SendTime);
public sealed record PageResult<T>(IReadOnlyList<T> Items, int? NextAfterId);
public sealed record FriendshipResult(FriendshipSummary? Friendship);
public sealed record GroupResult(GroupSummary Group);
public sealed record MessageResult(MessageSummary Message);
public sealed record HistoryResult(IReadOnlyList<MessageSummary> Items, int? NextBeforeId);
public sealed record UserIdResult(int UserId);
public sealed record GroupMemberResult(int GroupId, int UserId);
