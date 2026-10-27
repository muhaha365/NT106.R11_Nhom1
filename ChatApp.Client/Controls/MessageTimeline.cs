using ChatApp.Shared.Contracts;

namespace ChatApp.Client.Controls;

// Each bubble keeps a native read-only text box so messages can be selected and copied.
internal sealed class MessageTimeline : UserControl
{
    private sealed record Row(int Id, bool Own, Panel Host, RoundedPanel Bubble, Label Caption, RichTextBox Body);
    private readonly Panel _viewport = new() { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
    private readonly VScrollBar _scroll = new() { Dock = DockStyle.Right, SmallChange = 48, Visible = false, AccessibleName = "Cuộn lịch sử trò chuyện" };
    private readonly List<Row> _rows = [];
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
        _viewport.Resize += (_, _) => LayoutRows();
        _scroll.ValueChanged += (_, _) => {
            var delta = _position - _scroll.Value;
            foreach (var row in _rows) row.Host.Top += delta;
            _position = _scroll.Value;
        };
        _viewport.MouseWheel += (_, e) => ScrollTo(_position - e.Delta);
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
        var anchor = _rows.FirstOrDefault(row => row.Host.Bottom > 0);
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
            var body = new MessageBody(delta => ScrollTo(_position - delta)) {
                Text = message.Content, ReadOnly = true, Multiline = true, WordWrap = true,
                BorderStyle = BorderStyle.None, ScrollBars = RichTextBoxScrollBars.None, DetectUrls = false,
                BackColor = fill, ForeColor = own ? Color.White : UiTheme.Ink, Font = Font,
                AccessibleName = $"Tin nhắn của {name}, {message.SendTime.ToLocalTime():dd/MM HH:mm}",
                TabStop = true
            };
            bubble.Controls.Add(caption);
            bubble.Controls.Add(body);
            host.Controls.Add(bubble);
            host.MouseWheel += (_, e) => ScrollTo(_position - e.Delta);
            bubble.MouseWheel += (_, e) => ScrollTo(_position - e.Delta);
            caption.MouseWheel += (_, e) => ScrollTo(_position - e.Delta);
            void RevealCaret()
            {
                if (body.IsDisposed || !body.Focused || !GetCaretPos(out var caret)) return;
                var top = host.Top + bubble.Top + body.Top + caret.Y;
                var bottom = top + body.Font.Height;
                var margin = 8 * DeviceDpi / 96;
                if (top < margin) ScrollTo(_position + top - margin);
                else if (bottom > _viewport.Height - margin) ScrollTo(_position + bottom - _viewport.Height + margin);
            }
            // The native caret is updated after the selection notification, so reveal it on the next UI tick.
            body.Enter += (_, _) => body.BeginInvoke((Action)RevealCaret);
            body.SelectionChanged += (_, _) => { if (body.Focused) body.BeginInvoke((Action)RevealCaret); };
            _viewport.Controls.Add(host);
            _rows.Add(new Row(message.Id, own, host, bubble, caption, body));
            plainText.AppendLine(caption.Text).AppendLine(message.Content).AppendLine();
        }
        Text = plainText.ToString();
        _empty.Text = "Chưa có tin nhắn\nGửi lời chào đầu tiên cho nhóm.";
        _empty.Visible = _rows.Count == 0;
        _viewport.ResumeLayout();
        LayoutRows();
        if (scrollToEnd && _rows.Count > 0) ScrollTo(_scroll.Maximum);
        else if (anchor is not null && _rows.FirstOrDefault(row => row.Id == anchor.Id) is { } restored)
            ScrollTo(restored.Host.Top + _position - offset);
        else ScrollTo(oldPosition);
    }

    private void ScrollTo(int value) => _scroll.Value = Math.Clamp(value, 0, Math.Max(0, _scroll.Maximum - _scroll.LargeChange + 1));

    private void LayoutRows()
    {
        if (_layingOut || _rows.Count == 0) return;
        _layingOut = true;
        try
        {
            var atBottom = _position >= Math.Max(0, _scroll.Maximum - _scroll.LargeChange + 1) - 2;
            int S(int value) => Math.Max(1, value * DeviceDpi / 96);
            var width = Math.Max(S(100), ClientSize.Width - _scroll.Width);
            var y = S(18);
            foreach (var row in _rows)
            {
                var maxWidth = Math.Max(S(90), width * 83 / 100);
                var measured = TextRenderer.MeasureText(row.Body.Text, row.Body.Font, new Size(maxWidth - S(28), int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPadding | TextFormatFlags.ExpandTabs);
                var bubbleWidth = Math.Min(maxWidth, Math.Max(S(170), measured.Width + S(32)));
                row.Body.SetBounds(S(14), S(34), bubbleWidth - S(28), Math.Max(S(25), measured.Height + S(8)));
                // Use the native editor's wrapped line positions, including Unicode and trailing newlines.
                row.Body.CreateControl();
                row.Body.Height = Math.Max(row.Body.Height, row.Body.GetPositionFromCharIndex(row.Body.TextLength).Y + row.Body.Font.Height + S(8));
                var height = row.Body.Bottom + S(12);
                row.Host.SetBounds(0, y - _position, width, height + S(12));
                row.Bubble.SetBounds(row.Own ? width - bubbleWidth - S(14) : S(14), 0, bubbleWidth, height);
                row.Caption.SetBounds(S(14), S(8), bubbleWidth - S(28), S(22));
                y += row.Host.Height;
            }
            _scroll.Maximum = y + S(12) - 1;
            _scroll.LargeChange = Math.Max(1, _viewport.ClientSize.Height);
            _scroll.Visible = y + S(12) > _viewport.ClientSize.Height;
            ScrollTo(atBottom ? _scroll.Maximum : _position);
        }
        finally { _layingOut = false; }
    }

    private sealed class MessageBody(Action<int> scroll) : RichTextBox
    {
        protected override void OnMouseWheel(MouseEventArgs e) => scroll(e.Delta);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool GetCaretPos(out Point point);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _captionFont.Dispose();
    }
}
