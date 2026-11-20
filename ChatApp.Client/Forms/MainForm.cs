using System.Text.Json;
using ChatApp.Shared;
using ChatApp.Shared.Contracts;
using ChatApp.Client.Controls;
using System.Windows.Forms.VisualStyles;

namespace ChatApp.Client.Forms;

public partial class MainForm : Form
{
    private readonly ProtocolClient _protocol;
    private readonly FriendsPanel _friends;
    private readonly GroupChatPanel _groups;
    private UserSummary? _currentUser;
    private bool _connecting;
    private bool _closing;
    private bool _loggingOut;
    private bool CanSendChat
    {
        get { return _protocol.IsConnected && _currentUser != null; }
    }

    public MainForm() : this(new ProtocolClient(), null) { }

    // Luồng login bàn giao kết nối đã xác thực; form sở hữu và đóng kết nối này.
    public MainForm(ProtocolClient protocol, UserSummary? currentUser)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        if (currentUser != null && (currentUser.Id <= 0 || !protocol.IsConnected))
        {
            throw new ArgumentException("Cần kết nối đã xác thực và user hợp lệ trước khi mở form.");
        }
        _protocol = protocol;
        _currentUser = currentUser;
        InitializeComponent();
        _friends = new FriendsPanel(protocol) { Dock = DockStyle.Fill, Name = "friendsPanel" };
        _groups = new GroupChatPanel(protocol) { Dock = DockStyle.Fill, Name = "groupChatPanel" };
        friendsHost.Controls.Add(_friends);
        groupsHost.Controls.Add(_groups);
        _friends.FriendSelected += OnFriendSelected;
        _protocol.EventReceived += OnPacket;
        _protocol.Disconnected += OnDisconnected;
        Shown += MainForm_Shown;
        UpdateConnectionState();
        if (_currentUser != null)
            SetStatus("Đã đăng nhập. Chọn bạn bè hoặc nhóm để bắt đầu.");
    }

    private async void MainForm_Shown(object? sender, EventArgs e)
    {
        await RefreshSessionAsync();
    }

    private void OnFriendSelected(UserSummary user)
    {
        string status = "Offline";
        if (user.IsOnline) status = "Đang online";
        SetStatus($"Đã chọn {user.DisplayName} (@{user.Username}) · {status}");
    }

    private async Task RefreshSessionAsync()
    {
        _friends.SetSession(_currentUser);
        _groups.SetSession(_currentUser);
        if (_currentUser != null && _protocol.IsConnected)
        {
            // Hai danh sách được tải cùng lúc; chỉ hoàn tất khi cả hai đã xử lý xong.
            await Task.WhenAll(_friends.RefreshAsync(), _groups.RefreshAsync());
        }
    }

    private async void button1_Click(object? sender, EventArgs e)
    {
        if (_connecting || _protocol.IsConnected) return;
        string host = txtHost.Text.Trim();
        if (Uri.CheckHostName(host) == UriHostNameType.Unknown)
        {
            SetStatus("Vui lòng nhập địa chỉ máy chủ hợp lệ.");
            txtHost.Focus();
            return;
        }
        _connecting = true;
        UpdateConnectionState();
        SetStatus("Đang kết nối…");
        try
        {
            await _protocol.ConnectAsync(host, (int)numPort.Value);
            if (_closing) return;
            SetStatus("Đã kết nối. Danh sách bạn bè và nhóm sẽ tải sau khi đăng nhập thành công.");
            await RefreshSessionAsync();
        }
        catch (Exception error) when (error is IOException || error is System.Net.Sockets.SocketException
            || error is TimeoutException || error is InvalidOperationException)
        {
            SetStatus("Không kết nối được: " + error.Message);
        }
        finally
        {
            _connecting = false;
            UpdateConnectionState();
        }
    }

    private async void btn_send_Click(object? sender, EventArgs e)
    {
        string message = txtMessage.Text;
        if (string.IsNullOrWhiteSpace(message))
        {
            txtMessage.Focus();
            return;
        }
        if (!CanSendChat)
        {
            SetStatus("Bạn cần đăng nhập trước khi gửi tin nhắn.");
            return;
        }
        btn_send.Enabled = false;
        try
        {
            await _protocol.SendLegacyChatAsync(message);
            if (_closing) return;
            rtbChat.AppendText($"Bạn: {message}{Environment.NewLine}");
            if (txtMessage.Text == message) txtMessage.Clear();
        }
        catch (Exception error) when (error is IOException || error is InvalidOperationException
            || error is System.Net.Sockets.SocketException)
        {
            SetStatus("Chưa gửi được: " + error.Message);
        }
        finally
        {
            if (!_closing) btn_send.Enabled = CanSendChat;
        }
    }

    private void DemoMessage_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true;
            if (btn_send.Enabled) btn_send.PerformClick();
        }
    }

    private void OnPacket(Packet packet)
    {
        OnUi(() => DisplayPacket(packet));
    }

    private void DisplayPacket(Packet packet)
    {
        if (packet.Type == "chat" && packet.Payload.HasValue)
        {
            JsonElement value = packet.Payload.Value;
            if (value.ValueKind == JsonValueKind.String)
            {
                rtbChat.AppendText(value.GetString() + Environment.NewLine);
            }
        }
        _friends.HandleEvent(packet);
        _groups.HandleEvent(packet);
    }

    private void OnDisconnected(string reason)
    {
        OnUi(() => ShowDisconnected(reason));
    }

    private void ShowDisconnected(string reason)
    {
        _currentUser = null;
        _friends.SetSession(null);
        _groups.SetSession(null);
        UpdateConnectionState();
        SetStatus(reason);
    }

    private void OnUi(Action action)
    {
        if (_closing || IsDisposed || Disposing || !IsHandleCreated)
        {
            return;
        }
        if (InvokeRequired)
        {
            try
            {
                // Dữ liệu TCP có thể tới từ luồng nền; cập nhật control trên luồng giao diện.
                // Gọi lại OnUi ở luồng giao diện để kiểm tra form còn mở rồi mới chạy action.
                BeginInvoke(new Action(() => OnUi(action)));
            }
            catch (InvalidOperationException) when (_closing || IsDisposed || !IsHandleCreated)
            {
                // Form đã đóng trong lúc đang chuyển việc cập nhật sang luồng giao diện.
            }
        }
        else
        {
            action();
        }
    }

    private void UpdateConnectionState()
    {
        if (_closing) return;
        bool connected = _protocol.IsConnected;
        button1.Enabled = !_connecting && !connected;
        button1.Text = "Kết nối";
        if (_connecting) button1.Text = "Đang kết nối…";
        btnDisconnect.Enabled = connected;
        txtHost.Enabled = !connected && !_connecting;
        numPort.Enabled = txtHost.Enabled;
        btn_send.Enabled = CanSendChat;
        txtMessage.Enabled = CanSendChat;
        if (connected)
        {
            lblConnectionStatus.Text = "Đã kết nối";
            lblConnectionStatus.ForeColor = UiTheme.Accent;
        }
        else
        {
            lblConnectionStatus.Text = "Chưa kết nối";
            lblConnectionStatus.ForeColor = UiTheme.Muted;
        }
        lblCurrentUser.Text = "Chưa đăng nhập";
        if (_currentUser != null)
            lblCurrentUser.Text = $"{_currentUser.DisplayName} · @{_currentUser.Username}";
    }

    private void SetStatus(string text)
    {
        if (!_closing) lblStatus.Text = text;
    }
    private async void btnLogout_Click(object? sender, EventArgs e)
    {
        if(_loggingOut || _closing) return;

        _loggingOut = true;
        Enabled = false;
        SetStatus("Đang đăng xuất...");
        
        try
        {
            if(_protocol.IsConnected && _currentUser != null)
            {
                await _protocol.RequestAsync<EmptyResult>("auth.logout.request", new { });
            }
        }
        catch (Exception error)
        {
            if(!_closing && !IsDisposed && !Disposing)
            {
                MessageBox.Show(this, $"Chưa nhận được xác nhận đăng xuất từ server. Ứng dụng sẽ đóng kết nối và quay về đăng nhập. {error.Message}");
            }
        }
        finally
        {
            DisposeConnection();
            if(!IsDisposed && !Disposing)
            {
                Close();
            }
        }
    }
    private void DisposeConnection()
    {
        if (_closing) return;
        _closing = true;
        if (_protocol is not null)
        {
            _protocol.EventReceived -= OnPacket;
            _protocol.Disconnected -= OnDisconnected;
            _protocol.Dispose();
        }
    }
}
