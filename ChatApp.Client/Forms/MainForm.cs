using System.Text.Json;
using ChatApp.Shared;
using ChatApp.Shared.Contracts;
using ChatApp.Client.Controls;

namespace ChatApp.Client.Forms;

public partial class MainForm : Form
{
    private readonly ProtocolClient _protocol;
    private readonly FriendsPanel _friends;
    private readonly GroupChatPanel _groups;
    private UserSummary? _currentUser;
    private bool _connecting;
    private bool _closing;
    private bool CanSendChat =>_protocol.IsConnected && _currentUser is not null;

    public MainForm() : this(new ProtocolClient(), null) { }

    // Luồng login bàn giao kết nối đã xác thực; form sở hữu và đóng kết nối này.
    public MainForm(ProtocolClient protocol, UserSummary? currentUser)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        if (currentUser is not null && (currentUser.Id <= 0 || !protocol.IsConnected))
            throw new ArgumentException("Cần kết nối đã xác thực và user hợp lệ trước khi mở form.");
        _protocol = protocol;
        _currentUser = currentUser;
        InitializeComponent();
        _friends = new FriendsPanel(protocol) { Dock = DockStyle.Fill, Name = "friendsPanel" };
        _groups = new GroupChatPanel(protocol) { Dock = DockStyle.Fill, Name = "groupChatPanel" };
        friendsHost.Controls.Add(_friends);
        groupsHost.Controls.Add(_groups);
        _friends.FriendSelected += user => SetStatus($"Đã chọn {user.DisplayName} (@{user.Username}) · {(user.IsOnline ? "Đang online" : "Offline")}");
        _protocol.EventReceived += OnPacket;
        _protocol.Disconnected += OnDisconnected;
        Shown += async (_, _) => await RefreshSessionAsync();
        UpdateConnectionState();
        if (_currentUser is not null) SetStatus("Đã đăng nhập. Chọn bạn bè hoặc nhóm để bắt đầu.");
    }

    private async Task RefreshSessionAsync()
    {
        _friends.SetSession(_currentUser);
        _groups.SetSession(_currentUser);
        if (_currentUser is not null && _protocol.IsConnected)
            await Task.WhenAll(_friends.RefreshAsync(), _groups.RefreshAsync());
    }

    private async void button1_Click(object? sender, EventArgs e)
    {
        if (_connecting || _protocol.IsConnected) return;
        var host = txtHost.Text.Trim();
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
        catch (Exception error) when (error is IOException or System.Net.Sockets.SocketException or TimeoutException or InvalidOperationException)
        { if (!_closing) SetStatus("Không kết nối được: " + error.Message); }
        finally { _connecting = false; if (!_closing) UpdateConnectionState(); }
    }

    private async void btn_send_Click(object? sender, EventArgs e)
    {
        string message = txtMessage.Text;
        if (string.IsNullOrWhiteSpace(message)) { txtMessage.Focus(); return; }
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
        catch (Exception error) when (error is IOException or InvalidOperationException or System.Net.Sockets.SocketException)
        { if (!_closing) SetStatus("Chưa gửi được: " + error.Message); }
        finally
        {
            if (!_closing)
            {
                btn_send.Enabled = CanSendChat;
            }
        }
    }

    private void DemoMessage_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !e.Shift)
        { e.SuppressKeyPress = true; if (btn_send.Enabled) btn_send.PerformClick(); }
    }

    private void OnPacket(Packet packet) => OnUi(() =>
    {
        if (packet.Type == "chat" && packet.Payload is JsonElement value && value.ValueKind == JsonValueKind.String)
            rtbChat.AppendText(value.GetString() + Environment.NewLine);
        _friends.HandleEvent(packet);
        _groups.HandleEvent(packet);
    });

    private void OnDisconnected(string reason) => OnUi(() =>
    {
        _currentUser = null;
        _friends.SetSession(null);
        _groups.SetSession(null);
        UpdateConnectionState();
        SetStatus(reason);
    });

    private void OnUi(Action action)
    {
        if (_closing || IsDisposed || Disposing || !IsHandleCreated) return;
        if (InvokeRequired)
        {
            try { BeginInvoke((Action)(() => { if (!_closing && !IsDisposed) action(); })); }
            catch (InvalidOperationException) when (_closing || IsDisposed || !IsHandleCreated) { }
        }
        else action();
    }

    private void UpdateConnectionState()
    {
        bool connected = _protocol.IsConnected;
        button1.Enabled = !_connecting && !connected;
        button1.Text = _connecting ? "Đang kết nối…" : "Kết nối";
        btnDisconnect.Enabled = connected;
        txtHost.Enabled = numPort.Enabled = !connected && !_connecting;
        btn_send.Enabled = txtMessage.Enabled = CanSendChat;
        lblConnectionStatus.Text = connected ? "Đã kết nối" : "Chưa kết nối";
        lblConnectionStatus.ForeColor = connected ? UiTheme.Accent : UiTheme.Muted;
        lblCurrentUser.Text = _currentUser is null ? "Chưa đăng nhập" : $"{_currentUser.DisplayName} · @{_currentUser.Username}";
    }

    private void SetStatus(string text) => lblStatus.Text = text;

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
