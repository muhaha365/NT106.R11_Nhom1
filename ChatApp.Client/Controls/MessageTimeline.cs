using ChatApp.Shared.Contracts;

namespace ChatApp.Client.Controls;

// Mỗi tin là một khung chứa tên người gửi và RichTextBox chỉ đọc để chọn/copy nội dung.
internal sealed class MessageTimeline : UserControl
{
    private sealed record Row(int Id, bool Own, Panel Host, RoundedPanel Bubble, Label Caption, RichTextBox Body);
    private readonly Panel _viewport = new() { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
    private readonly VScrollBar _scroll = new() { Dock = DockStyle.Right, SmallChange = 48, Visible = false, AccessibleName = "Cuộn lịch sử trò chuyện" };
    private readonly List<Row> _rows = new List<Row>();
    private readonly Font _captionFont = new("Segoe UI", 8.5f);
    private readonly Label _empty = new() {
        Text = "Chọn nhóm để bắt đầu trò chuyện\nTin nhắn và lịch sử sẽ xuất hiện tại đây.",
        TextAlign = ContentAlignment.MiddleCenter, ForeColor = UiTheme.Muted, Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 10), AccessibleName = "Chưa chọn cuộc trò chuyện"
    };
    private bool _layingOut;
    private int _position;

    public MessageTimeline()
    {
        Dock = DockStyle.Fill;
        BackColor = UiTheme.Surface;
        Controls.Add(_viewport);
        Controls.Add(_scroll);
        _viewport.Controls.Add(_empty);
        _viewport.Resize += Viewport_Resize;
        _scroll.ValueChanged += Scroll_ValueChanged;
        _viewport.MouseWheel += Message_MouseWheel;
    }

    private void Viewport_Resize(object? sender, EventArgs e) { LayoutRows(); }

    private void Scroll_ValueChanged(object? sender, EventArgs e)
    {
        int delta = _position - _scroll.Value;
        foreach (Row row in _rows) row.Host.Top += delta;
        _position = _scroll.Value;
    }

    private void Message_MouseWheel(object? sender, MouseEventArgs e)
    {
        ScrollTo(_position - e.Delta);
    }

    public void Clear()
    {
        foreach (var row in _rows) row.Host.Dispose();
        _rows.Clear();
        Text = "";
        _position = 0;
        _scroll.Value = 0;
        _scroll.Visible = false;
        _empty.Text = "Chọn nhóm để bắt đầu trò chuyện\nTin nhắn và lịch sử sẽ xuất hiện tại đây.";
        _empty.Visible = true;
    }

    public void SetMessages(IEnumerable<MessageSummary> messages, int? selfId, Func<int, string> senderName, bool scrollToEnd)
    {
        Row? anchor = null;
        foreach (Row row in _rows)
        {
            if (row.Host.Bottom > 0) { anchor = row; break; }
        }
        var offset = anchor?.Host.Top ?? 0;
        var oldPosition = _position;
        _viewport.SuspendLayout();
        Clear();
        var plainText = new System.Text.StringBuilder();
        foreach (var message in messages)
        {
            var own = message.SenderId == selfId;
            var name = own ? "Bạn" : senderName(message.SenderId);
            var fill = own ? UiTheme.Accent : Color.White;
            var host = new Panel { BackColor = UiTheme.Surface, TabStop = false };
            var bubble = new RoundedPanel { FillColor = fill };
            var caption = new Label {
                Text = $"{name}  ·  {message.SendTime.ToLocalTime():dd/MM HH:mm}",
                ForeColor = own ? Color.FromArgb(227, 231, 255) : UiTheme.Muted,
                BackColor = fill, Font = _captionFont, AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var body = new MessageBody {
                Text = message.Content, ReadOnly = true, Multiline = true, WordWrap = true,
                BorderStyle = BorderStyle.None, ScrollBars = RichTextBoxScrollBars.None, DetectUrls = false,
                BackColor = fill, ForeColor = own ? Color.White : UiTheme.Ink, Font = Font,
                AccessibleName = $"Tin nhắn của {name}, {message.SendTime.ToLocalTime():dd/MM HH:mm}",
                TabStop = true
            };
            bubble.Controls.Add(caption);
            bubble.Controls.Add(body);
            host.Controls.Add(bubble);
            host.MouseWheel += Message_MouseWheel;
            bubble.MouseWheel += Message_MouseWheel;
            caption.MouseWheel += Message_MouseWheel;
            body.Scrolled += Message_MouseWheel;
            body.Enter += Body_SelectionChanged;
            body.SelectionChanged += Body_SelectionChanged;
            _viewport.Controls.Add(host);
            _rows.Add(new Row(message.Id, own, host, bubble, caption, body));
            plainText.AppendLine(caption.Text).AppendLine(message.Content).AppendLine();
        }
        Text = plainText.ToString();
        _empty.Text = "Chưa có tin nhắn\nGửi lời chào đầu tiên cho nhóm.";
        _empty.Visible = _rows.Count == 0;
        _viewport.ResumeLayout();
        LayoutRows();
        if (scrollToEnd && _rows.Count > 0)
        {
            ScrollTo(_scroll.Maximum);
            return;
        }
        // Khi thêm tin cũ phía trên, giữ nguyên tin mà người dùng đang đọc.
        foreach (Row row in _rows)
        {
            if (anchor != null && row.Id == anchor.Id)
            {
                ScrollTo(row.Host.Top + _position - offset);
                return;
            }
        }
        ScrollTo(oldPosition);
    }

    private void Body_SelectionChanged(object? sender, EventArgs e)
    {
        RichTextBox? body = sender as RichTextBox;
        if (body == null || !body.Focused) return;
        // Chờ Windows cập nhật con trỏ rồi cuộn tới vị trí đang chọn bằng bàn phím.
        body.BeginInvoke(new Action(() => RevealCaret(body)));
    }

    private void RevealCaret(RichTextBox body)
    {
        if (body.IsDisposed || !body.Focused || !GetCaretPos(out Point caret)) return;
        Control? bubble = body.Parent;
        Control? host = bubble?.Parent;
        if (bubble == null || host == null) return;
        int top = host.Top + bubble.Top + body.Top + caret.Y;
        int bottom = top + body.Font.Height;
        int margin = ScalePixels(8);
        if (top < margin) ScrollTo(_position + top - margin);
        else if (bottom > _viewport.Height - margin) ScrollTo(_position + bottom - _viewport.Height + margin);
    }

    private void ScrollTo(int value)
    {
        int lastPosition = Math.Max(0, _scroll.Maximum - _scroll.LargeChange + 1);
        _scroll.Value = Math.Clamp(value, 0, lastPosition);
    }

    private void LayoutRows()
    {
        if (_layingOut || _rows.Count == 0) return;
        _layingOut = true;
        try
        {
            var atBottom = _position >= Math.Max(0, _scroll.Maximum - _scroll.LargeChange + 1) - 2;
            var width = Math.Max(ScalePixels(100), ClientSize.Width - _scroll.Width);
            var y = ScalePixels(18);
            foreach (var row in _rows)
            {
                var maxWidth = Math.Max(ScalePixels(90), width * 83 / 100);
                var measured = TextRenderer.MeasureText(row.Body.Text, row.Body.Font, new Size(maxWidth - ScalePixels(28), int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPadding | TextFormatFlags.ExpandTabs);
                var bubbleWidth = Math.Min(maxWidth, Math.Max(ScalePixels(170), measured.Width + ScalePixels(32)));
                row.Body.SetBounds(ScalePixels(14), ScalePixels(34), bubbleWidth - ScalePixels(28), Math.Max(ScalePixels(25), measured.Height + ScalePixels(8)));
                // Đo cả dòng tự xuống hàng và dòng trống cuối để nội dung không bị cắt.
                row.Body.CreateControl();
                row.Body.Height = Math.Max(row.Body.Height, row.Body.GetPositionFromCharIndex(row.Body.TextLength).Y + row.Body.Font.Height + ScalePixels(8));
                var height = row.Body.Bottom + ScalePixels(12);
                row.Host.SetBounds(0, y - _position, width, height + ScalePixels(12));
                row.Bubble.SetBounds(row.Own ? width - bubbleWidth - ScalePixels(14) : ScalePixels(14), 0, bubbleWidth, height);
                row.Caption.SetBounds(ScalePixels(14), ScalePixels(8), bubbleWidth - ScalePixels(28), ScalePixels(22));
                y += row.Host.Height;
            }
            _scroll.Maximum = y + ScalePixels(12) - 1;
            _scroll.LargeChange = Math.Max(1, _viewport.ClientSize.Height);
            _scroll.Visible = y + ScalePixels(12) > _viewport.ClientSize.Height;
            ScrollTo(atBottom ? _scroll.Maximum : _position);
        }
        finally { _layingOut = false; }
    }

    // Quy đổi kích thước ở màn hình 100% sang mức phóng to hiện tại của Windows.
    private int ScalePixels(int pixels) { return Math.Max(1, pixels * DeviceDpi / 96); }

    private sealed class MessageBody : RichTextBox
    {
        public event EventHandler<MouseEventArgs>? Scrolled;

        // Cuộn cả danh sách tin nhắn, không cuộn riêng bên trong một RichTextBox.
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            Scrolled?.Invoke(this, e);
        }
    }

    // Lấy vị trí con trỏ chọn chữ từ Windows, chỉ phục vụ cuộn giao diện.
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool GetCaretPos(out Point point);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _captionFont.Dispose();
    }
}
