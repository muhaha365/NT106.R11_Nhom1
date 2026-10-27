using ChatApp.Client.Controls;

namespace ChatApp.Client.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = new System.ComponentModel.Container();
    private Panel friendsHost = null!;
    private Panel groupsHost = null!;
    private RichTextBox rtbChat = null!;
    private TextBox txtMessage = null!;
    private TextBox txtHost = null!;
    private NumericUpDown numPort = null!;
    private Button btn_send = null!;
    private Button button1 = null!;
    private Button btnDisconnect = null!;
    private Label lblConnectionStatus = null!;
    private Label lblCurrentUser = null!;
    private Label lblStatus = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) { DisposeConnection(); components?.Dispose(); }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10F);
        ForeColor = UiTheme.Ink;
        BackColor = UiTheme.Surface;
        ClientSize = new Size(1260, 790);
        MinimumSize = new Size(1050, 680);
        StartPosition = FormStartPosition.CenterScreen;
        Name = "MainForm";
        Text = "NEXUS X · Bạn bè và nhóm";

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(0), Margin = new Padding(0) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        Controls.Add(root);
        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(24, 8, 24, 8), Margin = new Padding(0) };
        header.Controls.Add(new Label { Name = "headerSubtitle", Text = "Không gian trò chuyện của bạn", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, AutoEllipsis = true, ForeColor = UiTheme.Muted, Font = new Font("Segoe UI", 10) });
        header.Controls.Add(new Label { Name = "brandLabel", Text = "nexus x", Dock = DockStyle.Left, Width = 200, Font = new Font("Segoe UI", 21F, FontStyle.Bold), ForeColor = UiTheme.Accent, TextAlign = ContentAlignment.MiddleLeft });
        root.Controls.Add(header, 0, 0);

        var connection = new FlowLayoutPanel { Name = "connectionBar", Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(20, 6, 20, 6), Margin = new Padding(0), BackColor = UiTheme.Surface, Font = new Font("Segoe UI", 9) };
        connection.Controls.Add(new Label { Text = "Máy chủ", AutoSize = true, Margin = new Padding(0, 8, 8, 0) });
        txtHost = new TextBox { Name = "serverHostBox", Text = "127.0.0.1", Width = 145, AccessibleName = "Địa chỉ máy chủ", Margin = new Padding(0, 4, 12, 0) };
        connection.Controls.Add(txtHost);
        connection.Controls.Add(new Label { Text = "Cổng", AutoSize = true, Margin = new Padding(0, 8, 8, 0) });
        numPort = new NumericUpDown { Name = "serverPortBox", Minimum = 1, Maximum = 65535, Value = 5000, Width = 78, AccessibleName = "Cổng máy chủ", Margin = new Padding(0, 4, 12, 0) };
        connection.Controls.Add(numPort);
        button1 = UiTheme.CreateButton("Kết nối", button1_Click, true);
        button1.Name = "connectButton";
        connection.Controls.Add(button1);
        btnDisconnect = UiTheme.CreateButton("Ngắt kết nối", (_, _) => _protocol.Disconnect());
        btnDisconnect.Name = "disconnectButton";
        connection.Controls.Add(btnDisconnect);
        lblConnectionStatus = new Label { Name = "connectionStatus", Text = "Chưa kết nối", AutoSize = true, Margin = new Padding(8, 8, 16, 0) };
        lblCurrentUser = new Label { Name = "currentUserLabel", Text = "Chưa đăng nhập", AutoSize = true, ForeColor = UiTheme.Muted, Margin = new Padding(0, 8, 0, 0) };
        connection.Controls.Add(lblConnectionStatus);
        connection.Controls.Add(lblCurrentUser);
        root.Controls.Add(connection, 0, 1);

        var workspace = new SplitContainer { Name = "workspaceSplit", Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1, SplitterWidth = 10, Margin = new Padding(16, 6, 16, 4), Size = new Size(1228, 590), SplitterDistance = 310, Panel1MinSize = 280, Panel2MinSize = 640 };
        friendsHost = new RoundedPanel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        workspace.Panel1.Controls.Add(friendsHost);
        var conversationTabs = new PageTabs { Name = "conversationTabs", Dock = DockStyle.Fill };
        var groupsTab = new TabPage("Chat nhóm") { BackColor = Color.White, Padding = new Padding(10), Name = "groupsTab" };
        groupsHost = new Panel { Dock = DockStyle.Fill };
        groupsTab.Controls.Add(groupsHost);
        var demoTab = new TabPage("Chat chung (demo)") { BackColor = Color.White, Padding = new Padding(14), Name = "demoTab" };
        var demoLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        demoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        demoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        demoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        demoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        demoLayout.Controls.Add(UiTheme.CreateHint("Tin nhắn ở đây được gửi tới mọi kết nối trong bản demo hiện tại."), 0, 0);
        rtbChat = new RichTextBox { Name = "legacyMessages", Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, DetectUrls = false };
        demoLayout.Controls.Add(rtbChat, 0, 1);
        var composer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 10, 0, 0) };
        composer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        composer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        txtMessage = new TextBox { Name = "legacyMessageBox", Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, AccessibleName = "Nội dung tin nhắn demo" };
        txtMessage.KeyDown += DemoMessage_KeyDown;
        btn_send = UiTheme.CreateButton("Gửi", btn_send_Click, true);
        btn_send.Name = "sendLegacyButton";
        btn_send.Dock = DockStyle.Fill;
        composer.Controls.Add(txtMessage, 0, 0);
        composer.Controls.Add(btn_send, 1, 0);
        demoLayout.Controls.Add(composer, 0, 2);
        demoTab.Controls.Add(demoLayout);
        conversationTabs.TabPages.Add(groupsTab);
        conversationTabs.TabPages.Add(demoTab);
        workspace.Panel2.Controls.Add(UiTheme.CreateTabs(conversationTabs));
        root.Controls.Add(workspace, 0, 2);
        lblStatus = new Label { Name = "mainStatus", Text = "Kết nối máy chủ để bắt đầu. Danh sách bạn bè và nhóm cần phiên đăng nhập.", Dock = DockStyle.Fill, AutoEllipsis = true, ForeColor = UiTheme.Muted, Font = new Font("Segoe UI", 9), Padding = new Padding(20, 6, 20, 6), Margin = new Padding(0), AccessibleRole = AccessibleRole.StatusBar };
        root.Controls.Add(lblStatus, 0, 3);
        ResumeLayout(true);
    }
}
