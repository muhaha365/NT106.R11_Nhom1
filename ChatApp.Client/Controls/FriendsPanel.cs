using System.Text;
using System.Text.Json;
using ChatApp.Shared;
using ChatApp.Shared.Contracts;

namespace ChatApp.Client.Controls;

public sealed class FriendsPanel : UserControl
{
    private readonly ProtocolClient _client;
    private readonly TabControl _tabs = new() { Name = "friendsTabs", Dock = DockStyle.Fill };
    private readonly ListView _friends = UiTheme.CreateList(("Tên hiển thị", 220), ("Tài khoản", 180), ("Trạng thái", 140));
    private readonly ListView _pending = UiTheme.CreateList(("Tên hiển thị", 210), ("Tài khoản", 170), ("Lời mời", 160));
    private readonly ListView _results = UiTheme.CreateList(("Tên hiển thị", 210), ("Tài khoản", 170), ("Trạng thái", 160));
    private readonly TextBox _searchBox = new() { Name = "searchBox", Dock = DockStyle.Fill, MaxLength = 100, AccessibleName = "Tên đăng nhập hoặc tên hiển thị" };
    private readonly Button _refresh = UiTheme.CreateButton("Tải lại");
    private readonly Button _remove = UiTheme.CreateButton("Xóa bạn");
    private readonly Button _accept = UiTheme.CreateButton("Chấp nhận", primary: true);
    private readonly Button _decline = UiTheme.CreateButton("Từ chối");
    private readonly Button _search = UiTheme.CreateButton("Tìm kiếm", primary: true);
    private readonly Button _add = UiTheme.CreateButton("Kết bạn", primary: true);
    private readonly Button _open = UiTheme.CreateButton("Chọn bạn", primary: true);
    private readonly Label _status = UiTheme.CreateHint("Đăng nhập để xem bạn bè.");
    private readonly Label _friendsHint = UiTheme.CreateHint("Đăng nhập để xem bạn bè.");
    private readonly Label _pendingHint = UiTheme.CreateHint("Đăng nhập để xem lời mời.");
    private readonly Label _searchHint = UiTheme.CreateHint("Nhập tên đăng nhập hoặc tên hiển thị để tìm người.");
    private readonly Dictionary<int, FriendEntry> _accepted = new();
    private readonly Dictionary<int, FriendEntry> _invitations = new();
    private readonly Dictionary<int, UserSummary> _found = new();
    private readonly Dictionary<int, bool> _presence = new();
    private CancellationTokenSource _sessionCancellation = new();
    private CancellationTokenSource? _searchCancellation;
    private UserSummary? _user;
    private Task? _refreshTask;
    private int _sessionVersion;
    private bool _refreshing, _refreshAgain, _mutating, _searching, _needsRefresh, _disposed;

    public event Action<UserSummary>? FriendSelected;

    public FriendsPanel(ProtocolClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        Dock = DockStyle.Fill;
        BackColor = UiTheme.Surface;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Inherit;
        Padding = new Padding(16);
        _friends.Name = "friendsList";
        _pending.Name = "pendingList";
        _results.Name = "searchResults";
        _refresh.Name = "refreshFriendsButton";
        _remove.Name = "removeFriendButton";
        _accept.Name = "acceptFriendButton";
        _decline.Name = "declineFriendButton";
        _search.Name = "searchButton";
        _add.Name = "addFriendButton";
        _status.Name = "friendsStatus";
        _status.Dock = DockStyle.Fill;
        _status.MaximumSize = Size.Empty;
        _status.AutoSize = true;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(UiTheme.CreateTitle("Bạn bè"), 0, 0);
        layout.Controls.Add(UiTheme.CreateActions(_refresh), 0, 1);
        layout.Controls.Add(_tabs, 0, 2);
        layout.Controls.Add(_status, 0, 3);
        Controls.Add(layout);
        AddListTab("Bạn bè", _friendsHint, _friends, _open, _remove);
        AddListTab("Lời mời", _pendingHint, _pending, _accept, _decline);
        BuildSearchTab();

        _refresh.Click += async (_, _) => await RefreshAsync();
        _search.Click += async (_, _) => await SearchAsync();
        _add.Click += async (_, _) => await AddAsync();
        _remove.Click += async (_, _) => await RemoveAsync();
        _accept.Click += async (_, _) => await RespondAsync(true);
        _decline.Click += async (_, _) => await RespondAsync(false);
        _open.Click += (_, _) => OpenSelectedFriend();
        _friends.DoubleClick += (_, _) => OpenSelectedFriend();
        _friends.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; OpenSelectedFriend(); } };
        _searchBox.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; if (_search.Enabled) await SearchAsync(); } };
        _searchBox.TextChanged += (_, _) => UpdateActions();
        foreach (var list in new[] { _friends, _pending, _results })
        {
            list.SelectedIndexChanged += (_, _) => UpdateActions();
            list.Resize += (_, _) => FitColumns(list);
        }
        UpdateActions();
    }

    private void AddListTab(string title, Label hint, ListView list, params Control[] actions)
    {
        var tab = new TabPage(title) { BackColor = UiTheme.Surface, Padding = new Padding(10) };
        var layout = PageLayout();
        hint.Dock = DockStyle.Fill;
        hint.MaximumSize = Size.Empty;
        layout.Controls.Add(hint, 0, 0);
        layout.Controls.Add(list, 0, 1);
        layout.Controls.Add(UiTheme.CreateActions(actions), 0, 2);
        tab.Controls.Add(layout);
        _tabs.TabPages.Add(tab);
    }

    private static TableLayoutPanel PageLayout()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        return layout;
    }

    private void BuildSearchTab()
    {
        var tab = new TabPage("Tìm người") { BackColor = UiTheme.Surface, Padding = new Padding(10) };
        var layout = PageLayout();
        var query = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 3 };
        query.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        query.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        query.Controls.Add(UiTheme.CreateHint("Tên đăng nhập hoặc tên hiển thị (1–50 ký tự)"), 0, 0);
        query.SetColumnSpan(query.GetControlFromPosition(0, 0)!, 2);
        query.Controls.Add(_searchBox, 0, 1);
        query.Controls.Add(_search, 1, 1);
        _searchHint.MaximumSize = Size.Empty;
        _searchHint.Dock = DockStyle.Fill;
        query.Controls.Add(_searchHint, 0, 2);
        query.SetColumnSpan(_searchHint, 2);
        layout.Controls.Add(query, 0, 0);
        layout.Controls.Add(_results, 0, 1);
        layout.Controls.Add(UiTheme.CreateActions(_add), 0, 2);
        tab.Controls.Add(layout);
        _tabs.TabPages.Add(tab);
    }

    public void SetSession(UserSummary? user)
    {
        if (_disposed) return;
        _sessionVersion++;
        _sessionCancellation.Cancel();
        _sessionCancellation.Dispose();
        _sessionCancellation = new CancellationTokenSource();
        _searchCancellation?.Cancel();
        _user = user;
        _refreshTask = null;
        _refreshing = _refreshAgain = _mutating = _searching = _needsRefresh = false;
        _accepted.Clear();
        _invitations.Clear();
        _found.Clear();
        _presence.Clear();
        RenderLists();
        _searchHint.Text = "Nhập tên đăng nhập hoặc tên hiển thị để tìm người.";
        SetStatus(Ready ? "Chọn Tải lại để xem danh sách bạn bè và lời mời." : UnavailableText);
        if (Ready)
        {
            _friendsHint.Text = "Chọn Tải lại để xem bạn bè.";
            _pendingHint.Text = "Chọn Tải lại để xem lời mời đã gửi và đã nhận.";
        }
        UpdateActions();
    }

    private bool Ready => !_disposed && _user is not null && _client.IsConnected;
    private bool Current(int version) => !_disposed && version == _sessionVersion;
    private string UnavailableText => _user is null ? "Đăng nhập để sử dụng danh sách bạn bè." : "Mất kết nối. Kết nối và đăng nhập lại để tải bạn bè.";

    public Task RefreshAsync()
    {
        if (_disposed) return Task.CompletedTask;
        if (!Ready) { SetStatus(UnavailableText); UpdateActions(); return Task.CompletedTask; }
        if (_mutating) { _refreshAgain = true; return Task.CompletedTask; }
        if (_refreshTask is { IsCompleted: false }) { _refreshAgain = true; return _refreshTask; }
        _refreshTask = RefreshCoreAsync(_sessionVersion, _sessionCancellation.Token);
        return _refreshTask;
    }

    private async Task RefreshCoreAsync(int version, CancellationToken token)
    {
        _refreshing = true;
        UpdateActions();
        try
        {
            do
            {
                _refreshAgain = false;
                SetStatus("Đang tải bạn bè và lời mời…");
                _friendsHint.Text = "Đang tải danh sách bạn bè…";
                _pendingHint.Text = "Đang tải lời mời đã nhận và đã gửi…";
                var accepted = await ReadFriendsAsync("Accepted", token);
                var pending = await ReadFriendsAsync("Pending", token);
                if (!Current(version)) return;
                _accepted.Clear();
                _invitations.Clear();
                foreach (var item in accepted) _accepted[item.User.Id] = item;
                foreach (var item in pending) _invitations[item.User.Id] = item;
                _needsRefresh = false;
                RenderLists();
                SetStatus($"{_accepted.Count} bạn bè · {_invitations.Count} lời mời.");
            } while (_refreshAgain && Current(version) && Ready);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (Current(version))
            {
                _friendsHint.Text = "Chưa tải xong bạn bè. Chọn Tải lại để đồng bộ.";
                _pendingHint.Text = "Chưa tải xong lời mời. Chọn Tải lại để đồng bộ.";
                ReportError(error, false);
            }
        }
        finally
        {
            if (Current(version)) { _refreshing = false; _refreshTask = null; UpdateActions(); }
        }
    }

    private async Task<List<FriendEntry>> ReadFriendsAsync(string status, CancellationToken token)
    {
        var items = new Dictionary<int, FriendEntry>();
        int? after = null;
        do
        {
            var page = await _client.RequestAsync<PageResult<FriendEntry>>("friend.list.request", new { Status = status, Limit = 50, AfterId = after }, token);
            token.ThrowIfCancellationRequested();
            if (page.Items is null) throw new InvalidDataException("Danh sách bạn bè không hợp lệ.");
            foreach (var item in page.Items)
            {
                if (item is null || item.User is null || item.Friendship is null || item.User.Id <= 0 || item.Friendship.Status != status)
                    throw new InvalidDataException("Dữ liệu quan hệ bạn bè không hợp lệ.");
                items[item.User.Id] = item;
            }
            after = NextCursor(after, page.NextAfterId, page.Items.Count);
        } while (after is not null);
        return items.Values.ToList();
    }

    private static int? NextCursor(int? previous, int? next, int count)
    {
        if (next is not null && (count == 0 || next <= (previous ?? 0)))
            throw new InvalidDataException("Server trả trang dữ liệu lặp. Hãy tải lại danh sách.");
        return next;
    }

    private async Task SearchAsync()
    {
        if (!_search.Enabled || !Ready) return;
        var query = _searchBox.Text.Trim();
        if (query.EnumerateRunes().Count() is < 1 or > 50 || query.Any(char.IsControl))
        {
            _searchHint.Text = "Nhập từ 1 đến 50 ký tự, không chứa ký tự điều khiển.";
            _searchBox.Focus();
            return;
        }
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_sessionCancellation.Token);
        _searchCancellation = cancellation;
        var token = cancellation.Token;
        var version = _sessionVersion;
        _searching = true;
        _found.Clear();
        RenderSearch();
        _searchHint.Text = "Đang tìm người…";
        UpdateActions();
        try
        {
            var found = new Dictionary<int, UserSummary>();
            int? after = null;
            do
            {
                var page = await _client.RequestAsync<PageResult<UserSummary>>("user.search.request", new { Query = query, Limit = 50, AfterId = after }, token);
                token.ThrowIfCancellationRequested();
                if (page.Items is null) throw new InvalidDataException("Kết quả tìm kiếm không hợp lệ.");
                foreach (var person in page.Items)
                {
                    if (person is null || person.Id <= 0) throw new InvalidDataException("Thông tin người dùng không hợp lệ.");
                    if (person.Id != _user?.Id) found[person.Id] = person;
                }
                after = NextCursor(after, page.NextAfterId, page.Items.Count);
            } while (after is not null);
            if (!Current(version) || cancellation != _searchCancellation) return;
            foreach (var item in found) _found[item.Key] = item.Value;
            RenderSearch();
            _searchHint.Text = found.Count == 0 ? "Không tìm thấy người phù hợp." : $"Tìm thấy {found.Count} người. Chọn một người để gửi lời mời.";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (Current(version)) { _searchHint.Text = "Tìm kiếm chưa hoàn tất. Hãy thử lại."; ReportError(error, false); }
        }
        finally
        {
            if (Current(version) && cancellation == _searchCancellation) { _searching = false; UpdateActions(); }
        }
    }

    private async Task AddAsync()
    {
        if (!_add.Enabled || Selected<UserSummary>(_results) is not { } user) return;
        await MutateAsync(async token =>
        {
            var result = await _client.RequestAsync<FriendshipResult>("friend.add.request", new { UserId = user.Id }, token);
            return () => ApplyFriendship(user, result.Friendship, "Pending");
        }, "Đang gửi lời mời…", "Đã gửi lời mời kết bạn.");
    }

    private async Task RespondAsync(bool accept)
    {
        if (!(accept ? _accept.Enabled : _decline.Enabled) || Selected<FriendEntry>(_pending) is not { } entry) return;
        await MutateAsync(async token =>
        {
            var result = await _client.RequestAsync<FriendshipResult>("friend.respond.request", new { UserId = entry.Friendship.UserInvite, Accept = accept }, token);
            return () => ApplyFriendship(entry.User, result.Friendship, accept ? "Accepted" : null);
        }, "Đang trả lời lời mời…", accept ? "Đã chấp nhận lời mời." : "Đã từ chối lời mời.");
    }

    private async Task RemoveAsync()
    {
        if (!_remove.Enabled || Selected<FriendEntry>(_friends) is not { } entry) return;
        var sessionVersion = _sessionVersion;
        if (MessageBox.Show(this, $"Xóa {entry.User.DisplayName} khỏi danh sách bạn bè?", "Xóa bạn", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes || !Current(sessionVersion) || !Ready) return;
        await MutateAsync(async token =>
        {
            var result = await _client.RequestAsync<UserIdResult>("friend.remove.request", new { UserId = entry.User.Id }, token);
            return () =>
            {
                if (result.UserId != entry.User.Id) throw new InvalidDataException("Phản hồi xóa bạn không khớp người đã chọn.");
                _accepted.Remove(result.UserId);
            };
        }, "Đang xóa bạn…", "Đã xóa bạn.");
    }

    private async Task MutateAsync(Func<CancellationToken, Task<Action>> request, string loading, string success)
    {
        if (!Ready || _mutating || _refreshing || _needsRefresh) return;
        var version = _sessionVersion;
        var token = _sessionCancellation.Token;
        _mutating = true;
        SetStatus(loading);
        UpdateActions();
        try
        {
            var apply = await request(token);
            if (!Current(version)) return;
            apply();
            RenderLists();
            SetStatus(success);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception error) { if (Current(version)) ReportError(error, true); }
        finally
        {
            if (Current(version))
            {
                _mutating = false;
                UpdateActions();
                if (_refreshAgain && Ready) { _refreshAgain = false; _ = RefreshAsync(); }
            }
        }
    }

    private void ApplyFriendship(UserSummary user, FriendshipSummary? friendship, string? expectedStatus)
    {
        if (friendship?.Status != expectedStatus) throw new InvalidDataException("Server chưa xác nhận đúng thay đổi quan hệ bạn bè.");
        if (friendship is not null &&
            (friendship.Status is not ("Accepted" or "Pending") || _user is null ||
             !((friendship.UserInvite == _user.Id && friendship.UserReceive == user.Id) ||
               (friendship.UserReceive == _user.Id && friendship.UserInvite == user.Id))))
            throw new InvalidDataException("Server trả quan hệ bạn bè không hợp lệ.");
        _accepted.Remove(user.Id);
        _invitations.Remove(user.Id);
        if (friendship is not null)
            (friendship.Status == "Accepted" ? _accepted : _invitations)[user.Id] = new FriendEntry(user, friendship);
    }

    public void HandleEvent(Packet packet)
    {
        if (!Ready) return;
        if (packet.Type == "friend.changed.event")
        {
            if (_mutating) _refreshAgain = true;
            else _ = RefreshAsync();
        }
        else if (packet.Type == "presence.changed.event" && packet.Payload is JsonElement payload && payload.ValueKind == JsonValueKind.Object &&
                 payload.TryGetProperty("UserId", out var id) && id.ValueKind == JsonValueKind.Number && id.TryGetInt32(out var userId) && userId > 0 &&
                 payload.TryGetProperty("IsOnline", out var online) && online.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            _presence[userId] = online.GetBoolean();
            RenderLists();
        }
    }

    private UserSummary WithPresence(UserSummary user) => _presence.TryGetValue(user.Id, out var online) ? user with { IsOnline = online } : user;
    private static string PresenceText(UserSummary user) => user.IsOnline ? "Online" : "Offline";
    private static T? Selected<T>(ListView list) where T : class => list.SelectedItems.Count == 0 ? null : list.SelectedItems[0].Tag as T;

    private void RenderLists()
    {
        FillList(_friends, _accepted.Values.OrderBy(x => x.User.Id).Select(entry =>
        {
            var current = entry with { User = WithPresence(entry.User) };
            return new ListViewItem(new[] { current.User.DisplayName, current.User.Username, PresenceText(current.User) }) { Tag = current };
        }));
        FillList(_pending, _invitations.Values.OrderBy(x => x.User.Id).Select(entry =>
        {
            var current = entry with { User = WithPresence(entry.User) };
            return new ListViewItem(new[] { current.User.DisplayName, current.User.Username, current.Friendship.UserReceive == _user?.Id ? "Đã nhận" : "Đã gửi" }) { Tag = current };
        }));
        _friendsHint.Text = !Ready ? UnavailableText : _accepted.Count == 0 ? "Chưa có bạn bè. Mở Tìm người để gửi lời mời." : $"{_accepted.Count} bạn bè. Chọn một người để xem thông tin.";
        var incoming = _invitations.Values.Count(x => x.Friendship.UserReceive == _user?.Id);
        _pendingHint.Text = !Ready ? UnavailableText : _invitations.Count == 0 ? "Chưa có lời mời kết bạn." : $"{incoming} lời mời đã nhận · {_invitations.Count - incoming} lời mời đã gửi. Chỉ có thể trả lời lời mời đã nhận.";
        RenderSearch();
        UpdateActions();
    }

    private void RenderSearch()
    {
        FillList(_results, _found.Values.OrderBy(x => x.Id).Select(person =>
        {
            var user = WithPresence(person);
            var relationship = _accepted.ContainsKey(user.Id) ? " · Bạn bè" : _invitations.ContainsKey(user.Id) ? " · Đã có lời mời" : "";
            return new ListViewItem(new[] { user.DisplayName, user.Username, PresenceText(user) + relationship }) { Tag = user };
        }));
        UpdateActions();
    }

    private static void FillList(ListView list, IEnumerable<ListViewItem> rows)
    {
        var selectedId = list.SelectedItems.Count == 0 ? (int?)null : list.SelectedItems[0].Tag switch { FriendEntry friend => friend.User.Id, UserSummary user => user.Id, _ => null };
        list.BeginUpdate();
        try
        {
            list.Items.Clear();
            foreach (var row in rows)
            {
                row.ToolTipText = string.Join(" · ", row.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(item => item.Text));
                list.Items.Add(row);
                var id = row.Tag switch { FriendEntry friend => friend.User.Id, UserSummary user => user.Id, _ => (int?)null };
                if (selectedId is not null && id == selectedId) row.Selected = true;
            }
        }
        finally { list.EndUpdate(); }
    }

    private static void FitColumns(ListView list)
    {
        var width = Math.Max(150, list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
        list.Columns[0].Width = width * 36 / 100;
        list.Columns[1].Width = width * 33 / 100;
        list.Columns[2].Width = width - list.Columns[0].Width - list.Columns[1].Width;
    }

    private void OpenSelectedFriend()
    {
        if (_open.Enabled && Selected<FriendEntry>(_friends) is { } entry) FriendSelected?.Invoke(WithPresence(entry.User));
    }

    private void UpdateActions()
    {
        if (_disposed) return;
        var idle = Ready && !_mutating && !_refreshing && !_searching;
        _refresh.Enabled = Ready && !_mutating && !_refreshing;
        _searchBox.Enabled = Ready && !_mutating && !_searching;
        _search.Enabled = idle && !string.IsNullOrWhiteSpace(_searchBox.Text);
        var canChange = idle && !_needsRefresh;
        _remove.Enabled = canChange && Selected<FriendEntry>(_friends) is not null;
        _open.Enabled = Ready && !_needsRefresh && Selected<FriendEntry>(_friends) is not null;
        var incoming = Selected<FriendEntry>(_pending) is { } entry && entry.Friendship.Status == "Pending" && entry.Friendship.UserReceive == _user?.Id;
        _accept.Enabled = _decline.Enabled = canChange && incoming;
        var selected = Selected<UserSummary>(_results);
        _add.Enabled = canChange && selected is not null && selected.Id != _user?.Id && !_accepted.ContainsKey(selected.Id) && !_invitations.ContainsKey(selected.Id);
    }

    private void SetStatus(string text) { _status.Text = text; _status.AccessibleName = text; }

    private void ReportError(Exception error, bool mutation)
    {
        if (error is ProtocolRequestException { Code: "NOT_AUTHENTICATED" })
        {
            SetSession(null);
            SetStatus("Phiên đăng nhập không còn hợp lệ. Hãy đăng nhập lại.");
            return;
        }
        if (mutation && error is not ProtocolRequestException)
        {
            _needsRefresh = true;
            SetStatus("Kết quả thao tác chưa xác định. Hãy Tải lại dữ liệu trước khi thử lại. " + error.Message);
        }
        else if (error is ProtocolRequestException { Code: "UNKNOWN_TYPE" or "UNSUPPORTED_VERSION" })
            SetStatus("Server chưa hỗ trợ chức năng bạn bè này. " + error.Message);
        else SetStatus(error.Message + (Ready ? " Chọn Tải lại để đồng bộ danh sách." : " " + UnavailableText));
        UpdateActions();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _sessionVersion++;
            _sessionCancellation.Cancel();
            _sessionCancellation.Dispose();
            _searchCancellation?.Cancel();
            _searchCancellation?.Dispose();
        }
        base.Dispose(disposing);
    }
}
