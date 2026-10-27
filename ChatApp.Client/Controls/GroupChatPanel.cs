using System.Text;
using System.Text.Json;
using ChatApp.Client.Forms;
using ChatApp.Shared;
using ChatApp.Shared.Contracts;

namespace ChatApp.Client.Controls;

public sealed class GroupChatPanel : UserControl
{
    private readonly ProtocolClient _client;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource _sessionLifetime;
    private CancellationTokenSource _selectionLifetime;
    private readonly ListView groupsList = UiTheme.CreateList(("Nhóm của bạn", 240));
    private readonly Button createGroupButton;
    private readonly Button refreshGroupsButton;
    private readonly RichTextBox groupMessages = new() { Name = "groupMessages", Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, DetectUrls = false, AccessibleName = "Lịch sử trò chuyện nhóm" };
    private readonly TextBox groupMessageBox = new() { Name = "groupMessageBox", Dock = DockStyle.Fill, Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical, AccessibleName = "Nội dung tin nhắn nhóm" };
    private readonly Button sendGroupButton;
    private readonly Button membersButton;
    private readonly Button loadHistoryButton;
    private readonly Label groupStatusLabel = UiTheme.CreateHint("Kết nối và đăng nhập để sử dụng nhóm.");
    private readonly Label groupTitleLabel = UiTheme.CreateTitle("Trò chuyện nhóm");
    private readonly Dictionary<int, GroupSummary> _groups = new();
    private readonly Dictionary<int, MessageSummary> _messages = new();
    private readonly Dictionary<int, string> _memberNames = new();
    private readonly Dictionary<int, string> _drafts = new();
    private readonly HashSet<int> _removedGroups = new();
    private readonly HashSet<int> _historyCursors = new();
    private UserSummary? _user;
    private GroupSummary? _selected;
    private Form? _activeDialog;
    private int _sessionVersion;
    private int _selectionVersion;
    private int _namesVersion;
    private bool _listBusy;
    private bool _refreshQueued;
    private bool _renderingList;
    private bool _historyBusy;
    private bool _historyLoaded;
    private bool _sending;
    private bool _dialogOpen;
    private bool _sendNeedsRefresh;
    private bool _createNeedsRefresh;
    private int? _nextBeforeId;

    public GroupChatPanel(ProtocolClient client)
    {
        _client = client;
        _sessionLifetime = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _selectionLifetime = CancellationTokenSource.CreateLinkedTokenSource(_sessionLifetime.Token);
        Dock = DockStyle.Fill;
        Font = new Font("Segoe UI", 10);
        BackColor = UiTheme.Surface;
        Padding = new Padding(16);
        groupsList.Name = "groupsList";
        groupsList.AccessibleName = "Danh sách nhóm của bạn";
        createGroupButton = UiTheme.CreateButton("Tạo nhóm", (_, _) => CreateGroup(), true);
        createGroupButton.Name = "createGroupButton";
        refreshGroupsButton = UiTheme.CreateButton("Tải lại", async (_, _) => await RefreshAsync());
        refreshGroupsButton.Name = "refreshGroupsButton";
        sendGroupButton = UiTheme.CreateButton("Gửi", async (_, _) => await SendAsync(), true);
        sendGroupButton.Name = "sendGroupButton";
        membersButton = UiTheme.CreateButton("Thành viên", (_, _) => ShowMembers());
        membersButton.Name = "membersButton";
        loadHistoryButton = UiTheme.CreateButton("Tải lịch sử", async (_, _) => await LoadHistoryAsync());
        loadHistoryButton.Name = "loadHistoryButton";
        groupStatusLabel.Name = "groupStatusLabel";
        groupStatusLabel.MaximumSize = Size.Empty;
        groupStatusLabel.Dock = DockStyle.Fill;
        groupTitleLabel.Name = "groupTitleLabel";
        groupTitleLabel.AutoEllipsis = true;
        groupTitleLabel.AutoSize = false;
        groupTitleLabel.Dock = DockStyle.Fill;
        groupTitleLabel.Height = 32;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(0, 0, 12, 0) };
        left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.Controls.Add(UiTheme.CreateTitle("Nhóm của bạn"), 0, 0);
        left.Controls.Add(UiTheme.CreateActions(createGroupButton, refreshGroupsButton), 0, 1);
        left.Controls.Add(groupsList, 0, 2);
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7 };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(groupTitleLabel, 0, 0);
        header.Controls.Add(membersButton, 1, 0);
        right.Controls.Add(header, 0, 0);
        right.Controls.Add(groupStatusLabel, 0, 1);
        right.Controls.Add(groupMessages, 0, 2);
        right.Controls.Add(UiTheme.CreateActions(loadHistoryButton), 0, 3);
        right.Controls.Add(UiTheme.CreateHint("Tin nhắn"), 0, 4);
        right.Controls.Add(groupMessageBox, 0, 5);
        right.Controls.Add(UiTheme.CreateActions(sendGroupButton, UiTheme.CreateHint("Enter để gửi · Shift+Enter để xuống dòng")), 0, 6);
        root.Controls.Add(left, 0, 0);
        root.Controls.Add(right, 1, 0);
        Controls.Add(root);
        groupsList.SelectedIndexChanged += (_, _) => {
            if (!_renderingList)
                SelectGroup(groupsList.SelectedItems.Count == 1 ? groupsList.SelectedItems[0].Tag as GroupSummary : null);
        };
        groupsList.Resize += (_, _) => {
            if (groupsList.IsHandleCreated) groupsList.BeginInvoke((Action)(() => {
                if (!groupsList.IsDisposed && groupsList.Columns.Count > 0)
                    groupsList.Columns[0].Width = Math.Max(60, groupsList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
            }));
        };
        groupMessageBox.TextChanged += (_, _) => UpdateActions();
        groupMessageBox.KeyDown += async (_, e) => {
            if (e.KeyCode != Keys.Enter || e.Shift) return;
            e.SuppressKeyPress = true;
            await SendAsync();
        };
        UpdateActions();
    }

    private bool Active => !IsDisposed && !Disposing && !_lifetime.IsCancellationRequested;
    private bool Authenticated => Active && _client.IsConnected && _user is not null;
    private bool CurrentSession(int version) => Authenticated && version == _sessionVersion;
    private bool CurrentSelection(int session, int selection, int groupId) => CurrentSession(session) && selection == _selectionVersion && _selected?.Id == groupId && !_removedGroups.Contains(groupId);

    public void SetSession(UserSummary? user)
    {
        if (!Active) return;
        _sessionVersion++;
        _selectionVersion++;
        _activeDialog?.Close();
        _activeDialog = null;
        _selectionLifetime.Cancel();
        _selectionLifetime.Dispose();
        _sessionLifetime.Cancel();
        _sessionLifetime.Dispose();
        _sessionLifetime = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _selectionLifetime = CancellationTokenSource.CreateLinkedTokenSource(_sessionLifetime.Token);
        _user = user;
        _selected = null;
        _groups.Clear();
        _messages.Clear();
        _memberNames.Clear();
        _drafts.Clear();
        _removedGroups.Clear();
        _historyCursors.Clear();
        _listBusy = _refreshQueued = _historyBusy = _historyLoaded = _sending = _dialogOpen = _sendNeedsRefresh = _createNeedsRefresh = false;
        _nextBeforeId = null;
        groupsList.Items.Clear();
        groupMessages.Clear();
        groupMessageBox.Clear();
        groupTitleLabel.Text = "Trò chuyện nhóm";
        groupStatusLabel.Text = Authenticated ? "Tải lại danh sách nhóm để bắt đầu." : "Kết nối và đăng nhập để sử dụng nhóm.";
        UpdateActions();
    }

    public async Task RefreshAsync()
    {
        if (!Authenticated) { if (Active) { groupStatusLabel.Text = "Kết nối và đăng nhập để sử dụng nhóm."; UpdateActions(); } return; }
        if (_listBusy) { _refreshQueued = true; return; }
        var session = _sessionVersion;
        var token = _sessionLifetime.Token;
        _listBusy = true;
        UpdateActions();
        try
        {
            do
            {
                _refreshQueued = false;
                groupStatusLabel.Text = "Đang tải danh sách nhóm…";
                var groups = new Dictionary<int, GroupSummary>();
                var cursors = new HashSet<int>();
                int? cursor = null;
                do
                {
                    if (!CurrentSession(session)) return;
                    var page = await _client.RequestAsync<PageResult<GroupSummary>>("group.list.request", new { Limit = 50, AfterId = cursor }, token);
                    if (!CurrentSession(session)) return;
                    foreach (var group in page.Items)
                        if (!_removedGroups.Contains(group.Id)) groups[group.Id] = group;
                    if (page.NextAfterId is { } next && (next <= (cursor ?? 0) || !cursors.Add(next)))
                        throw new InvalidDataException("Server trả con trỏ danh sách nhóm không hợp lệ.");
                    cursor = page.NextAfterId;
                } while (cursor is not null);
                if (!CurrentSession(session)) return;
                // Events received during a snapshot trigger one more complete refresh.
                if (_refreshQueued) continue;
                _groups.Clear();
                foreach (var group in groups.Values)
                    if (!_removedGroups.Contains(group.Id)) _groups[group.Id] = group;
                _createNeedsRefresh = false;
                RenderGroups();
                groupStatusLabel.Text = _groups.Count == 0 ? "Bạn chưa có nhóm. Chọn Tạo nhóm để bắt đầu." : _selected is null ? "Chọn một nhóm để xem lịch sử và gửi tin nhắn." : $"{_groups.Count} nhóm · Chọn Thành viên để xem danh sách.";
            } while (_refreshQueued && CurrentSession(session));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception error) { if (CurrentSession(session)) groupStatusLabel.Text = error.Message; }
        finally
        {
            if (Active && session == _sessionVersion)
            {
                _listBusy = false;
                UpdateActions();
                if (_refreshQueued && Authenticated) { _refreshQueued = false; _ = RefreshAsync(); }
            }
        }
    }

    private void RenderGroups()
    {
        _renderingList = true;
        groupsList.BeginUpdate();
        try
        {
            groupsList.Items.Clear();
            foreach (var group in _groups.Values.OrderBy(group => group.Id))
            {
                var row = new ListViewItem(group.GroupName) { Tag = group };
                groupsList.Items.Add(row);
                if (_selected?.Id == group.Id) { _selected = group; row.Selected = true; groupTitleLabel.Text = group.GroupName; }
            }
        }
        finally { groupsList.EndUpdate(); _renderingList = false; }
        if (_selected is not null && !_groups.ContainsKey(_selected.Id)) SelectGroup(null);
        UpdateActions();
    }

    private void SelectGroup(GroupSummary? group)
    {
        if (_selected?.Id == group?.Id) return;
        if (_activeDialog is GroupMembersDialog) _activeDialog.Close();
        if (_selected is { } previous) _drafts[previous.Id] = groupMessageBox.Text;
        _selectionVersion++;
        _selectionLifetime.Cancel();
        _selectionLifetime.Dispose();
        _selectionLifetime = CancellationTokenSource.CreateLinkedTokenSource(_sessionLifetime.Token);
        _selected = group;
        _messages.Clear();
        _memberNames.Clear();
        _historyCursors.Clear();
        _nextBeforeId = null;
        _historyBusy = _historyLoaded = _sending = _sendNeedsRefresh = false;
        groupMessages.Clear();
        groupMessageBox.Text = group is not null && _drafts.TryGetValue(group.Id, out var draft) ? draft : "";
        groupTitleLabel.Text = group?.GroupName ?? "Trò chuyện nhóm";
        groupStatusLabel.Text = group is null ? "Chọn một nhóm để xem lịch sử và gửi tin nhắn." : "Đang tải lịch sử nhóm…";
        UpdateActions();
        if (group is not null && Authenticated)
        {
            _ = LoadHistoryAsync();
            _ = LoadMemberNamesAsync();
        }
    }

    private async Task LoadHistoryAsync()
    {
        if (!Authenticated || _selected is not { } group || _historyBusy || (_historyLoaded && _nextBeforeId is null && !_sendNeedsRefresh)) return;
        var session = _sessionVersion;
        var selection = _selectionVersion;
        var token = _selectionLifetime.Token;
        bool syncingUnknown = _sendNeedsRefresh;
        var before = _historyLoaded && !syncingUnknown ? _nextBeforeId : null;
        if (syncingUnknown) _historyCursors.Clear();
        _historyBusy = true;
        groupStatusLabel.Text = "Đang tải lịch sử nhóm…";
        UpdateActions();
        try
        {
            var result = await _client.RequestAsync<HistoryResult>("chat.history.request", new { PeerId = (int?)null, GroupId = group.Id, BeforeId = before, Limit = 10 }, token);
            if (!CurrentSelection(session, selection, group.Id)) return;
            if (result.NextBeforeId is { } next && (next <= 0 || (before is { } old && next >= old) || !_historyCursors.Add(next)))
                throw new InvalidDataException("Server trả con trỏ lịch sử không hợp lệ.");
            foreach (var message in result.Items) MergeMessage(message);
            _nextBeforeId = result.NextBeforeId;
            _historyLoaded = true;
            if (syncingUnknown) _sendNeedsRefresh = false;
            RenderMessages(before is null);
            groupStatusLabel.Text = syncingUnknown ? "Đã đồng bộ lịch sử. Kiểm tra tin nhắn đã lưu trước khi gửi lại bản nháp." : _messages.Count == 0 ? "Nhóm chưa có tin nhắn. Hãy gửi lời chào đầu tiên." : _nextBeforeId is null ? "Đã tải hết lịch sử nhóm." : "Có thể tải thêm tin nhắn cũ.";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception error) { if (CurrentSelection(session, selection, group.Id)) groupStatusLabel.Text = error.Message; }
        finally { if (CurrentSelection(session, selection, group.Id)) { _historyBusy = false; UpdateActions(); } }
    }

    private async Task LoadMemberNamesAsync()
    {
        if (!Authenticated || _selected is not { } group) return;
        var session = _sessionVersion;
        var selection = _selectionVersion;
        var namesVersion = ++_namesVersion;
        var token = _selectionLifetime.Token;
        var names = new Dictionary<int, string>();
        var cursors = new HashSet<int>();
        int? cursor = null;
        try
        {
            do
            {
                if (!CurrentSelection(session, selection, group.Id) || namesVersion != _namesVersion) return;
                var page = await _client.RequestAsync<PageResult<UserSummary>>("group.members.request", new { GroupId = group.Id, Limit = 50, AfterId = cursor }, token);
                if (!CurrentSelection(session, selection, group.Id) || namesVersion != _namesVersion) return;
                foreach (var member in page.Items) names[member.Id] = member.DisplayName;
                if (page.NextAfterId is { } next && (next <= (cursor ?? 0) || !cursors.Add(next)))
                    throw new InvalidDataException("Server trả con trỏ thành viên không hợp lệ.");
                cursor = page.NextAfterId;
            } while (cursor is not null);
            _memberNames.Clear();
            foreach (var entry in names) _memberNames[entry.Key] = entry.Value;
            RenderMessages(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception error) { if (CurrentSelection(session, selection, group.Id) && namesVersion == _namesVersion) groupStatusLabel.Text = "Không tải được tên thành viên: " + error.Message; }
    }

    private void MergeMessage(MessageSummary message)
    {
        if (_selected is not null && message.Id > 0 && message.GroupId == _selected.Id && message.ReceiverId is null)
            _messages[message.Id] = message;
    }

    private void RenderMessages(bool scrollToEnd)
    {
        var oldSelection = groupMessages.SelectionStart;
        groupMessages.Clear();
        foreach (var message in _messages.Values.OrderBy(message => message.Id))
        {
            var sender = message.SenderId == _user?.Id ? "Bạn" : _memberNames.GetValueOrDefault(message.SenderId, $"Người dùng #{message.SenderId}");
            groupMessages.AppendText($"{sender} · {message.SendTime.ToLocalTime():dd/MM/yyyy HH:mm}\n{message.Content}\n\n");
        }
        groupMessages.SelectionStart = scrollToEnd ? groupMessages.TextLength : Math.Min(oldSelection, groupMessages.TextLength);
        groupMessages.SelectionLength = 0;
        groupMessages.ScrollToCaret();
    }

    private async Task SendAsync()
    {
        if (!Authenticated || _selected is not { } group || _sending || _dialogOpen || !_historyLoaded || _sendNeedsRefresh || _removedGroups.Contains(group.Id)) return;
        var content = groupMessageBox.Text;
        if (string.IsNullOrWhiteSpace(content)) return;
        if (content.EnumerateRunes().Count() > 2000 || Encoding.UTF8.GetByteCount(content) > 4000 || content.Any(character => char.IsControl(character) && character is not ('\t' or '\r' or '\n')))
        {
            groupStatusLabel.Text = "Tin nhắn tối đa 2.000 ký tự và 4.000 byte UTF-8; không chứa ký tự điều khiển ngoài xuống dòng và tab.";
            return;
        }
        var session = _sessionVersion;
        var selection = _selectionVersion;
        var token = _selectionLifetime.Token;
        _sending = true;
        groupStatusLabel.Text = "Đang gửi tin nhắn…";
        UpdateActions();
        try
        {
            var result = await _client.RequestAsync<MessageResult>("chat.send.request", new { ReceiverId = (int?)null, GroupId = group.Id, Content = content }, token);
            if (!CurrentSelection(session, selection, group.Id)) return;
            if (result.Message.GroupId != group.Id || result.Message.ReceiverId is not null || result.Message.SenderId != _user!.Id)
                throw new InvalidDataException("Phản hồi tin nhắn không khớp nhóm đang gửi.");
            MergeMessage(result.Message);
            RenderMessages(true);
            if (groupMessageBox.Text == content) { groupMessageBox.Clear(); _drafts.Remove(group.Id); }
            groupStatusLabel.Text = "Tin nhắn đã được server lưu.";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (CurrentSelection(session, selection, group.Id))
            {
                _sendNeedsRefresh = error is TimeoutException or IOException;
                groupStatusLabel.Text = _sendNeedsRefresh ? "Kết quả gửi chưa xác định. Chọn Đồng bộ tin nhắn trước khi gửi tiếp." : error.Message;
            }
        }
        finally { if (CurrentSelection(session, selection, group.Id)) { _sending = false; UpdateActions(); } }
    }

    private void CreateGroup()
    {
        if (!Authenticated || _user is null || _dialogOpen || _sending || _listBusy || _createNeedsRefresh) return;
        var session = _sessionVersion;
        using var dialog = new CreateGroupDialog(_client, _user, _sessionLifetime.Token);
        _activeDialog = dialog;
        _dialogOpen = true;
        UpdateActions();
        try
        {
            if (dialog.ShowDialog(this) != DialogResult.OK || !CurrentSession(session) || dialog.CreatedGroup is not { } group || _removedGroups.Contains(group.Id)) return;
            _groups[group.Id] = group;
            RenderGroups();
            SelectGroup(group);
            _renderingList = true;
            foreach (ListViewItem row in groupsList.Items) row.Selected = (row.Tag as GroupSummary)?.Id == group.Id;
            _renderingList = false;
            _ = RefreshAsync();
        }
        finally
        {
            if (Active && session == _sessionVersion)
            {
                _activeDialog = null;
                _dialogOpen = false;
                if (dialog.NeedsRefresh) _createNeedsRefresh = true;
                UpdateActions();
                if (dialog.NeedsRefresh) _ = RefreshAsync();
            }
        }
    }

    private void ShowMembers()
    {
        if (!Authenticated || _user is null || _selected is not { } group || _dialogOpen || _sending) return;
        var session = _sessionVersion;
        var selection = _selectionVersion;
        using var dialog = new GroupMembersDialog(_client, group, _user, _selectionLifetime.Token);
        _activeDialog = dialog;
        _dialogOpen = true;
        UpdateActions();
        try
        {
            dialog.ShowDialog(this);
            if (CurrentSelection(session, selection, group.Id) && (dialog.MembershipChanged || dialog.NeedsRefresh)) _ = LoadMemberNamesAsync();
        }
        finally { if (Active && session == _sessionVersion) { _activeDialog = null; _dialogOpen = false; UpdateActions(); } }
    }

    public void HandleEvent(Packet packet)
    {
        if (!Authenticated || packet.Payload is not { ValueKind: JsonValueKind.Object } payload) return;
        try
        {
            if (packet.Type == "chat.message.event" && payload.TryGetProperty("Message", out var messageData))
            {
                var message = messageData.Deserialize<MessageSummary>();
                if (message is null || _selected is null || _removedGroups.Contains(_selected.Id) || message.GroupId != _selected.Id || message.ReceiverId is not null) return;
                MergeMessage(message);
                RenderMessages(true);
                return;
            }
            if (packet.Type != "group.changed.event") return;
            if (!payload.TryGetProperty("GroupId", out var groupIdData) || !groupIdData.TryGetInt32(out var groupId) || groupId <= 0 ||
                !payload.TryGetProperty("Action", out var actionData) || actionData.ValueKind != JsonValueKind.String) return;
            var action = actionData.GetString();
            if (action is not ("created" or "member_added" or "member_removed")) return;
            int? userId = payload.TryGetProperty("UserId", out var userData) && userData.ValueKind == JsonValueKind.Number && userData.TryGetInt32(out var id) ? id : null;
            if (action == "member_removed" && userId == _user!.Id)
            {
                _removedGroups.Add(groupId);
                _groups.Remove(groupId);
                _drafts.Remove(groupId);
                if (_selected?.Id == groupId)
                {
                    _activeDialog?.Close();
                    SelectGroup(null);
                    _drafts.Remove(groupId);
                    groupStatusLabel.Text = "Bạn đã bị xóa khỏi nhóm này.";
                }
                RenderGroups();
            }
            else
            {
                if (action == "member_added" && userId == _user!.Id) _removedGroups.Remove(groupId);
                if (!_removedGroups.Contains(groupId) && payload.TryGetProperty("Group", out var groupData))
                {
                    var group = groupData.Deserialize<GroupSummary>();
                    if (group is not null && group.Id == groupId) { _groups[groupId] = group; RenderGroups(); }
                }
                if (_selected?.Id == groupId) _ = LoadMemberNamesAsync();
            }
            _ = RefreshAsync();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or ArgumentException)
        {
            groupStatusLabel.Text = "Không đọc được sự kiện nhóm từ server. Hãy tải lại danh sách.";
        }
    }

    private void UpdateActions()
    {
        var authenticated = Authenticated;
        var selected = authenticated && _selected is not null && !_removedGroups.Contains(_selected.Id);
        groupsList.Enabled = authenticated && !_dialogOpen;
        refreshGroupsButton.Enabled = authenticated && !_listBusy && !_dialogOpen;
        createGroupButton.Enabled = authenticated && !_sending && !_dialogOpen && !_listBusy && !_createNeedsRefresh;
        membersButton.Enabled = selected && !_sending && !_dialogOpen;
        groupMessageBox.Enabled = selected && !_sending && !_dialogOpen && !_sendNeedsRefresh;
        sendGroupButton.Enabled = selected && !_sending && !_dialogOpen && _historyLoaded && !_sendNeedsRefresh && !string.IsNullOrWhiteSpace(groupMessageBox.Text);
        loadHistoryButton.Enabled = selected && !_historyBusy && !_dialogOpen && (!_historyLoaded || _nextBeforeId is not null || _sendNeedsRefresh);
        loadHistoryButton.Text = _historyBusy ? "Đang tải…" : _sendNeedsRefresh ? "Đồng bộ tin nhắn" : !_historyLoaded ? "Tải lịch sử" : "Tải tin nhắn cũ";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !IsDisposed)
        {
            _lifetime.Cancel();
            _activeDialog?.Close();
            _selectionLifetime.Cancel();
            _selectionLifetime.Dispose();
            _sessionLifetime.Cancel();
            _sessionLifetime.Dispose();
            _lifetime.Dispose();
        }
        base.Dispose(disposing);
    }
}
