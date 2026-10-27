using ChatApp.Client.Controls;
using ChatApp.Shared.Contracts;

namespace ChatApp.Client.Forms;

public sealed class GroupMembersDialog : Form
{
    private readonly ProtocolClient _client;
    private readonly GroupSummary _group;
    private readonly UserSummary _user;
    private readonly CancellationTokenSource _lifetime;
    private readonly ListView memberList = UiTheme.CreateList(("Thành viên", 260), ("Vai trò", 110));
    private readonly ComboBox _friends = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "DisplayName", AccessibleName = "Bạn bè để thêm vào nhóm" };
    private readonly Button addMemberButton;
    private readonly Button removeMemberButton;
    private readonly Label _status = UiTheme.CreateHint("Đang tải thành viên…");
    private readonly List<UserSummary> _members = new();
    private List<UserSummary> _acceptedFriends = new();
    private bool _busy;
    private bool _loaded;
    public bool MembershipChanged { get; private set; }
    public bool NeedsRefresh { get; private set; }

    public GroupMembersDialog(ProtocolClient client, GroupSummary group, UserSummary user, CancellationToken selectionToken)
    {
        _client = client;
        _group = group;
        _user = user;
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(selectionToken);
        Text = "Thành viên · " + group.GroupName;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        Font = new Font("Segoe UI", 10);
        BackColor = UiTheme.Surface;
        Size = new Size(540, 550);
        MinimumSize = new Size(420, 420);
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = MaximizeBox = false;
        memberList.Name = "memberList";
        memberList.AccessibleName = "Danh sách thành viên nhóm";
        addMemberButton = UiTheme.CreateButton("Thêm thành viên", async (_, _) => await MutateAsync(true), true);
        addMemberButton.Name = "addMemberButton";
        removeMemberButton = UiTheme.CreateButton("Xóa thành viên", async (_, _) => await MutateAsync(false));
        removeMemberButton.Name = "removeMemberButton";
        var close = UiTheme.CreateButton("Đóng", (_, _) => Close());
        close.DialogResult = DialogResult.Cancel;
        CancelButton = close;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 6 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        for (var i = 0; i < 4; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(UiTheme.CreateTitle("Thành viên nhóm"), 0, 0);
        layout.Controls.Add(memberList, 0, 1);
        var owner = group.CreatedBy == user.Id;
        var friendLabel = UiTheme.CreateHint("Thêm một bạn bè đã chấp nhận vào nhóm:");
        friendLabel.Visible = _friends.Visible = owner;
        layout.Controls.Add(friendLabel, 0, 2);
        layout.Controls.Add(_friends, 0, 3);
        layout.Controls.Add(_status, 0, 4);
        addMemberButton.Visible = removeMemberButton.Visible = owner;
        layout.Controls.Add(UiTheme.CreateActions(addMemberButton, removeMemberButton, close), 0, 5);
        Controls.Add(layout);
        _status.MaximumSize = Size.Empty;
        _status.Dock = DockStyle.Fill;
        memberList.SelectedIndexChanged += (_, _) => UpdateActions();
        _friends.SelectedIndexChanged += (_, _) => UpdateActions();
        Shown += async (_, _) => await LoadAsync();
        UpdateActions();
    }

    private bool Active => !IsDisposed && !Disposing && !_lifetime.IsCancellationRequested;

    private async Task LoadAsync()
    {
        _busy = true;
        _loaded = false;
        UpdateActions();
        try
        {
            var members = new Dictionary<int, UserSummary>();
            var cursors = new HashSet<int>();
            int? cursor = null;
            do
            {
                _lifetime.Token.ThrowIfCancellationRequested();
                if (!_client.IsConnected) throw new IOException("Chưa kết nối tới server.");
                var page = await _client.RequestAsync<PageResult<UserSummary>>("group.members.request", new { GroupId = _group.Id, Limit = 50, AfterId = cursor }, _lifetime.Token);
                foreach (var user in page.Items) members[user.Id] = user;
                if (page.NextAfterId is { } next && (next <= (cursor ?? 0) || !cursors.Add(next)))
                    throw new InvalidDataException("Server trả con trỏ thành viên không hợp lệ.");
                cursor = page.NextAfterId;
            } while (cursor is not null);
            if (!Active) return;
            _members.Clear();
            _members.AddRange(members.Values.OrderBy(user => user.Id));
            RenderMembers();
            if (_group.CreatedBy == _user.Id)
                _acceptedFriends = await GroupUiData.LoadFriendsAsync(_client, _user.Id, _lifetime.Token);
            if (!Active) return;
            RenderFriends();
            _loaded = true;
            _status.Text = $"{_members.Count}/20 thành viên." + (_group.CreatedBy == _user.Id ? " Chủ nhóm có thể thêm hoặc xóa thành viên." : "Chỉ chủ nhóm có thể quản lý thành viên.");
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception error) { if (Active) _status.Text = error.Message; }
        finally { if (Active) { _busy = false; UpdateActions(); } }
    }

    private void RenderMembers()
    {
        memberList.Items.Clear();
        foreach (var user in _members)
        {
            var row = new ListViewItem(user.DisplayName) { Tag = user };
            row.SubItems.Add(user.Id == _group.CreatedBy ? "Chủ nhóm" : "Thành viên");
            memberList.Items.Add(row);
        }
    }

    private void RenderFriends()
    {
        var ids = _members.Select(user => user.Id).ToHashSet();
        _friends.Items.Clear();
        _friends.Items.AddRange(_acceptedFriends.Where(user => !ids.Contains(user.Id)).Cast<object>().ToArray());
        if (_friends.Items.Count > 0) _friends.SelectedIndex = 0;
    }

    private void UpdateActions()
    {
        var allowed = Active && _client.IsConnected && _loaded && !_busy && _group.CreatedBy == _user.Id;
        var selected = memberList.SelectedItems.Count == 1 ? memberList.SelectedItems[0].Tag as UserSummary : null;
        addMemberButton.Enabled = allowed && _members.Count < 20 && _friends.SelectedItem is UserSummary;
        removeMemberButton.Enabled = allowed && selected is not null && selected.Id != _group.CreatedBy;
        _friends.Enabled = allowed;
    }

    private async Task MutateAsync(bool add)
    {
        if (!Active || _busy || !_loaded || !_client.IsConnected || _group.CreatedBy != _user.Id) return;
        var selected = add ? _friends.SelectedItem as UserSummary : memberList.SelectedItems.Count == 1 ? memberList.SelectedItems[0].Tag as UserSummary : null;
        if (selected is null || selected.Id == _group.CreatedBy || (add && _members.Count >= 20)) return;
        if (!add && MessageBox.Show(this, $"Xóa {selected.DisplayName} khỏi nhóm?", "Xác nhận xóa thành viên", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _busy = true;
        UpdateActions();
        _status.Text = add ? "Đang thêm thành viên…" : "Đang xóa thành viên…";
        try
        {
            var result = await _client.RequestAsync<GroupMemberResult>(add ? "group.member.add.request" : "group.member.remove.request", new { GroupId = _group.Id, UserId = selected.Id }, _lifetime.Token);
            if (!Active) return;
            if (result.GroupId != _group.Id || result.UserId != selected.Id) throw new InvalidDataException("Phản hồi thành viên không khớp yêu cầu.");
            MembershipChanged = true;
            await LoadAsync();
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (Active)
            {
                NeedsRefresh = error is TimeoutException or IOException;
                if (NeedsRefresh) _loaded = false;
                _status.Text = NeedsRefresh ? "Kết quả thay đổi chưa xác định. Đóng và mở lại danh sách thành viên để đồng bộ." : error.Message;
            }
        }
        finally { if (Active) { _busy = false; UpdateActions(); } }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !IsDisposed) { _lifetime.Cancel(); _lifetime.Dispose(); }
        base.Dispose(disposing);
    }
}
