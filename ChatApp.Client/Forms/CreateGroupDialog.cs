using System.Text;
using ChatApp.Client.Controls;
using ChatApp.Shared.Contracts;

namespace ChatApp.Client.Forms;

public sealed class CreateGroupDialog : Form
{
    private readonly ProtocolClient _client;
    private readonly UserSummary _user;
    private readonly CancellationTokenSource _lifetime;
    private readonly TextBox groupNameBox = new() { Name = "groupNameBox", Dock = DockStyle.Fill, AccessibleName = "Tên nhóm" };
    private readonly CheckedListBox groupFriendChoices = new() { Name = "groupFriendChoices", Dock = DockStyle.Fill, CheckOnClick = true, DisplayMember = "DisplayName", AccessibleName = "Bạn bè được thêm vào nhóm" };
    private readonly Button confirmCreateGroupButton;
    private readonly Label _status = UiTheme.CreateHint("Đang tải danh sách bạn bè…");
    private bool _busy;
    private bool _loaded;
    public GroupSummary? CreatedGroup { get; private set; }
    public bool NeedsRefresh { get; private set; }

    public CreateGroupDialog(ProtocolClient client, UserSummary user, CancellationToken sessionToken)
    {
        _client = client;
        _user = user;
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(sessionToken);
        Text = "Tạo nhóm";
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        Font = new Font("Segoe UI", 10);
        BackColor = UiTheme.Surface;
        ForeColor = UiTheme.Ink;
        groupFriendChoices.BorderStyle = BorderStyle.None;
        groupFriendChoices.BackColor = Color.White;
        groupNameBox.PlaceholderText = "Ví dụ: Nhóm đồ án NT106";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(400, 420);
        Size = new Size(480, 560);
        MinimizeBox = false;
        MaximizeBox = false;
        confirmCreateGroupButton = UiTheme.CreateButton("Tạo nhóm", CreateButton_Click, true);
        confirmCreateGroupButton.Name = "confirmCreateGroupButton";
        confirmCreateGroupButton.Enabled = false;
        var cancel = UiTheme.CreateButton("Hủy");
        cancel.DialogResult = DialogResult.Cancel;
        CancelButton = cancel;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 7 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(UiTheme.CreateTitle("Tên nhóm"), 0, 0);
        layout.Controls.Add(groupNameBox, 0, 1);
        layout.Controls.Add(UiTheme.CreateHint("Chọn tối đa 19 bạn bè. Bạn được tự động thêm làm chủ nhóm."), 0, 2);
        layout.Controls.Add(groupFriendChoices, 0, 3);
        layout.Controls.Add(_status, 0, 4);
        layout.Controls.Add(UiTheme.CreateActions(confirmCreateGroupButton, cancel), 0, 5);
        Controls.Add(layout);
        _status.MaximumSize = Size.Empty;
        _status.Dock = DockStyle.Fill;
        Shown += CreateGroupDialog_Shown;
        groupFriendChoices.ItemCheck += FriendChoices_ItemCheck;
    }

    private async void CreateGroupDialog_Shown(object? sender, EventArgs e)
    {
        await LoadFriendsAsync();
    }

    private async void CreateButton_Click(object? sender, EventArgs e)
    {
        await CreateAsync();
    }

    private void FriendChoices_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (e.NewValue == CheckState.Checked && groupFriendChoices.CheckedItems.Count >= 19)
        {
            e.NewValue = CheckState.Unchecked;
            _status.Text = "Chỉ có thể chọn tối đa 19 bạn bè.";
        }
    }

    private async Task LoadFriendsAsync()
    {
        try
        {
            var friends = await GroupUiData.LoadFriendsAsync(_client, _user.Id, _lifetime.Token);
            if (!Active) return;
            foreach (UserSummary friend in friends)
            {
                groupFriendChoices.Items.Add(friend);
            }
            _loaded = true;
            _status.Text = friends.Count == 0 ? "Chưa có bạn bè. Bạn vẫn có thể tạo nhóm chỉ có mình." : "Chọn thành viên từ danh sách bạn bè đã chấp nhận.";
            confirmCreateGroupButton.Enabled = true;
        }
        // Đóng cửa sổ hoặc mất phiên đăng nhập thì ngừng tải, không báo lỗi.
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception error) { if (Active) _status.Text = error.Message; }
    }

    private async Task CreateAsync()
    {
        if (_busy || !_loaded || NeedsRefresh || !Active || !_client.IsConnected) return;
        string name = groupNameBox.Text.Trim();
        bool hasControlCharacter = false;
        foreach (char character in name)
        {
            if (char.IsControl(character)) hasControlCharacter = true;
        }
        // Đếm ký tự Unicode để một emoji không bị tính thành hai ký tự C#.
        int characterCount = name.EnumerateRunes().Count();
        if (characterCount == 0 || characterCount > 100 || hasControlCharacter)
        {
            _status.Text = "Tên nhóm cần có 1–100 ký tự và không chứa ký tự điều khiển.";
            groupNameBox.Focus();
            return;
        }
        List<int> memberIds = new List<int>();
        foreach (UserSummary friend in groupFriendChoices.CheckedItems)
        {
            if (friend.Id != _user.Id && !memberIds.Contains(friend.Id))
            {
                memberIds.Add(friend.Id);
            }
        }
        if (memberIds.Count > 19)
        {
            _status.Text = "Chỉ có thể chọn tối đa 19 bạn bè.";
            return;
        }
        _busy = true;
        SetInputEnabled(false);
        _status.Text = "Đang tạo nhóm…";
        try
        {
            var request = new { GroupName = name, MemberIds = memberIds.ToArray() };
            var result = await _client.RequestAsync<GroupResult>("group.create.request", request, _lifetime.Token);
            if (!Active) return;
            CreatedGroup = result.Group;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (Active)
            {
                // Có thể server đã tạo nhóm nhưng phản hồi bị mất trên đường về.
                NeedsRefresh = error is TimeoutException || error is IOException;
                _status.Text = error.Message;
                if (NeedsRefresh)
                    _status.Text = "Kết quả tạo nhóm chưa xác định. Đóng cửa sổ và tải lại danh sách nhóm trước khi tạo tiếp.";
            }
        }
        finally
        {
            if (Active)
            {
                _busy = false;
                SetInputEnabled(_client.IsConnected && !NeedsRefresh);
            }
        }
    }

    private bool Active => !IsDisposed && !Disposing && !_lifetime.IsCancellationRequested;

    private void SetInputEnabled(bool enabled)
    {
        confirmCreateGroupButton.Enabled = enabled;
        groupNameBox.Enabled = enabled;
        groupFriendChoices.Enabled = enabled;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !IsDisposed) { _lifetime.Cancel(); _lifetime.Dispose(); }
        base.Dispose(disposing);
    }
}

internal static class GroupUiData
{
    internal static async Task<List<UserSummary>> LoadFriendsAsync(ProtocolClient client, int ownId, CancellationToken cancellationToken)
    {
        var friends = new Dictionary<int, UserSummary>();
        int? cursor = null;
        do
        {
            // RequestAsync kiểm tra kết nối và việc hủy yêu cầu.
            var page = await client.RequestAsync<PageResult<FriendEntry>>("friend.list.request", new { Status = "Accepted", Limit = 50, AfterId = cursor }, cancellationToken);
            foreach (FriendEntry entry in page.Items)
            {
                if (entry.User.Id != ownId && entry.Friendship.Status == "Accepted")
                {
                    friends[entry.User.Id] = entry.User;
                }
            }
            // ID tăng sau mỗi trang; không cần lưu thêm danh sách các ID đã đọc.
            if (page.NextAfterId.HasValue && page.NextAfterId.Value <= cursor.GetValueOrDefault())
            {
                throw new InvalidDataException("Server trả con trỏ danh sách bạn bè không hợp lệ.");
            }
            cursor = page.NextAfterId;
        } while (cursor is not null);
        return friends.Values.OrderBy(user => user.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
    }
}
