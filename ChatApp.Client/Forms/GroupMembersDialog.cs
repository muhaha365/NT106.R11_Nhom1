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
        ForeColor = UiTheme.Ink;
        Size = new Size(540, 550);
        MinimumSize = new Size(420, 420);
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = MaximizeBox = false;
        memberList.Name = "memberList";
        memberList.AccessibleName = "Danh sách thành viên nhóm";
        addMemberButton = UiTheme.CreateButton("Thêm thành viên", AddMemberButton_Click, true);
        addMemberButton.Name = "addMemberButton";
        removeMemberButton = UiTheme.CreateButton("Xóa thành viên", RemoveMemberButton_Click);
        removeMemberButton.Name = "removeMemberButton";
        var close = UiTheme.CreateButton("Đóng");
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
        memberList.SelectedIndexChanged += SelectionChanged;
        _friends.SelectedIndexChanged += SelectionChanged;
        Shown += GroupMembersDialog_Shown;
        UpdateActions();
    }

    private bool Active => !IsDisposed && !Disposing && !_lifetime.IsCancellationRequested;

    private async void GroupMembersDialog_Shown(object? sender, EventArgs e)
    {
        await LoadAsync();
    }

    private void SelectionChanged(object? sender, EventArgs e)
    {
        UpdateActions();
    }

    private async void AddMemberButton_Click(object? sender, EventArgs e)
    {
        UserSummary? friend = _friends.SelectedItem as UserSummary;
        if (friend == null || _members.Count >= 20) return;
        await ChangeMemberAsync(friend, "group.member.add.request", "Đang thêm thành viên…");
    }

    private async void RemoveMemberButton_Click(object? sender, EventArgs e)
    {
        if (!removeMemberButton.Enabled || memberList.SelectedItems.Count != 1) return;
        UserSummary? member = memberList.SelectedItems[0].Tag as UserSummary;
        if (member == null || member.Id == _group.CreatedBy) return;
        DialogResult answer = MessageBox.Show(this, $"Xóa {member.DisplayName} khỏi nhóm?",
            "Xác nhận xóa thành viên", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;
        await ChangeMemberAsync(member, "group.member.remove.request", "Đang xóa thành viên…");
    }

    private async Task LoadAsync(string? confirmation = null)
    {
        _busy = true;
        _loaded = false;
        UpdateActions();
        try
        {
            var members = new Dictionary<int, UserSummary>();
            int? cursor = null;
            do
            {
                var page = await _client.RequestAsync<PageResult<UserSummary>>("group.members.request", new { GroupId = _group.Id, Limit = 50, AfterId = cursor }, _lifetime.Token);
                foreach (UserSummary user in page.Items)
                {
                    members[user.Id] = user;
                }
                // Trang tiếp theo phải có ID lớn hơn trang trước, nên không thể lặp lại.
                if (page.NextAfterId.HasValue && page.NextAfterId.Value <= cursor.GetValueOrDefault())
                {
                    throw new InvalidDataException("Server trả con trỏ thành viên không hợp lệ.");
                }
                cursor = page.NextAfterId;
            } while (cursor is not null);
            if (!Active) return;
            _members.Clear();
            _members.AddRange(members.Values.OrderBy(user => user.Id));
            RenderMembers();
            _loaded = true;
            _status.Text = $"{_members.Count}/20 thành viên. ";
            if (_group.CreatedBy == _user.Id)
            {
                _friends.Items.Clear();
                var friends = await GroupUiData.LoadFriendsAsync(_client, _user.Id, _lifetime.Token);
                if (!Active) return;
                RenderFriends(friends);
                _status.Text += "Chủ nhóm có thể thêm hoặc xóa thành viên.";
            }
            else
                _status.Text += "Chỉ chủ nhóm có thể quản lý thành viên.";
        }
        // Đóng dialog hoặc đổi phiên thì bỏ kết quả đang chờ, không báo lỗi.
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (ProtocolRequestException error) when (error.Code is "NOT_IMPLEMENTED" or "UNKNOWN_TYPE")
        {
            if (Active)
                _status.Text = _loaded
                    ? "Server hiện chưa hỗ trợ danh sách bạn bè để thêm vào nhóm."
                    : "Server hiện chưa hỗ trợ danh sách thành viên.";
        }
        catch (Exception error) { if (Active) _status.Text = error.Message; }
        finally
        {
            if (Active)
            {
                if (confirmation is not null) _status.Text = confirmation + " " + _status.Text;
                _busy = false;
                UpdateActions();
            }
        }
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

    private void RenderFriends(List<UserSummary> friends)
    {
        foreach (UserSummary friend in friends)
        {
            if (!_members.Any(member => member.Id == friend.Id))
                _friends.Items.Add(friend);
        }
        if (_friends.Items.Count > 0) _friends.SelectedIndex = 0;
    }

    private void UpdateActions()
    {
        var allowed = Active && _client.IsConnected && _loaded && !_busy && _group.CreatedBy == _user.Id;
        UserSummary? selected = null;
        if (memberList.SelectedItems.Count == 1)
            selected = memberList.SelectedItems[0].Tag as UserSummary;
        addMemberButton.Enabled = allowed && _members.Count < 20 && _friends.SelectedItem is UserSummary;
        removeMemberButton.Enabled = allowed && selected is not null && selected.Id != _group.CreatedBy;
        _friends.Enabled = addMemberButton.Enabled;
    }

    // Hai nút chọn người khác nhau, nhưng dùng chung bước gửi và tải lại danh sách.
    private async Task ChangeMemberAsync(UserSummary selected, string requestType, string status)
    {
        if (!Active || _busy || !_loaded || !_client.IsConnected || _group.CreatedBy != _user.Id) return;
        if (selected.Id == _group.CreatedBy) return;
        _busy = true;
        UpdateActions();
        _status.Text = status;
        try
        {
            var request = new { GroupId = _group.Id, UserId = selected.Id };
            var result = await _client.RequestAsync<GroupMemberResult>(requestType, request, _lifetime.Token);
            if (!Active) return;
            if (result.GroupId != _group.Id || result.UserId != selected.Id) throw new InvalidDataException("Phản hồi thành viên không khớp yêu cầu.");
            MembershipChanged = true;
            await LoadAsync(requestType == "group.member.add.request"
                ? $"Đã thêm {selected.DisplayName}."
                : $"Đã xóa {selected.DisplayName}.");
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (Active)
            {
                NeedsRefresh = error is not ProtocolRequestException;
                if (NeedsRefresh) _loaded = false;
                _status.Text = error.Message;
                if (NeedsRefresh)
                    _status.Text = "Kết quả thay đổi chưa xác định. Đóng và mở lại danh sách thành viên để đồng bộ.";
            }
        }
        finally
        {
            if (Active)
            {
                _busy = false;
                UpdateActions();
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !IsDisposed) { _lifetime.Cancel(); _lifetime.Dispose(); }
        base.Dispose(disposing);
    }
}
