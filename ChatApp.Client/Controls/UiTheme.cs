using System.Drawing.Drawing2D;
using ChatApp.Shared.Contracts;

namespace ChatApp.Client.Controls;

internal static class UiTheme
{
    public static readonly Color Accent = Color.FromArgb(68, 83, 212);
    public static readonly Color AccentSoft = Color.FromArgb(237, 240, 255);
    public static readonly Color Surface = Color.FromArgb(245, 247, 251);
    public static readonly Color Ink = Color.FromArgb(29, 38, 60);
    public static readonly Color Muted = Color.FromArgb(92, 104, 127);
    public static readonly Color Line = Color.FromArgb(225, 230, 240);

    public static Button CreateButton(string text, EventHandler? click = null, bool primary = false)
    {
        var button = new RoundedButton(primary) {
            Text = text, AutoSize = true, MinimumSize = new Size(0, 36), Padding = new Padding(12, 5, 12, 5),
            Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0), AccessibleName = text
        };
        if (click is not null) button.Click += click;
        return button;
    }

    public static Label CreateTitle(string text) => new() {
        Text = text, AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold),
        ForeColor = Ink, Margin = new Padding(0, 0, 0, 10)
    };

    public static Label CreateHint(string text) => new() {
        Text = text, AutoSize = true, ForeColor = Muted, Margin = new Padding(0, 5, 0, 8),
        Font = new Font("Segoe UI", 9), MaximumSize = new Size(520, 0), AccessibleName = text
    };

    public static ListView CreateList(params (string Title, int Width)[] columns)
    {
        var list = new ContactList {
            Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false,
            HideSelection = false, ShowItemToolTips = true, HeaderStyle = ColumnHeaderStyle.None,
            BorderStyle = BorderStyle.None, BackColor = Color.White, Font = new Font("Segoe UI", 10)
        };
        foreach (var column in columns) list.Columns.Add(column.Title, column.Width);
        return list;
    }

    public static Control CreateTabs(TabControl tabs)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Color.White, Margin = Padding.Empty };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var navigation = new FlowLayoutPanel { Name = tabs.Name + "Navigation", Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, BackColor = Color.White, Margin = new Padding(0, 0, 0, 8) };
        for (var index = 0; index < tabs.TabCount; index++)
        {
            var pageIndex = index;
            var choice = new RadioButton {
                Text = tabs.TabPages[index].Text, AccessibleName = tabs.TabPages[index].Text,
                Appearance = Appearance.Button, FlatStyle = FlatStyle.Flat, AutoSize = true,
                UseVisualStyleBackColor = false,
                Padding = new Padding(12, 7, 12, 7), Margin = new Padding(0, 0, 4, 0),
                Cursor = Cursors.Hand, Checked = index == tabs.SelectedIndex, TextAlign = ContentAlignment.MiddleCenter
            };
            choice.FlatAppearance.BorderSize = 0;
            choice.FlatAppearance.CheckedBackColor = AccentSoft;
            choice.FlatAppearance.MouseOverBackColor = Surface;
            choice.ForeColor = choice.Checked ? Accent : Muted;
            choice.BackColor = choice.Checked ? AccentSoft : Color.White;
            choice.CheckedChanged += (_, _) => {
                choice.ForeColor = choice.Checked ? Accent : Muted;
                choice.BackColor = choice.Checked ? AccentSoft : Color.White;
                if (choice.Checked) tabs.SelectedIndex = pageIndex;
            };
            navigation.Controls.Add(choice);
        }
        void SyncNavigation()
        {
            for (var index = 0; index < navigation.Controls.Count; index++)
                ((RadioButton)navigation.Controls[index]).Checked = index == Math.Max(0, tabs.SelectedIndex);
        }
        tabs.SelectedIndexChanged += (_, _) => SyncNavigation();
        layout.Controls.Add(navigation, 0, 0);
        layout.Controls.Add(tabs, 0, 1);
        SyncNavigation();
        return layout;
    }

    public static FlowLayoutPanel CreateActions(params Control[] controls)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = new Padding(0, 8, 0, 0) };
        panel.Controls.AddRange(controls);
        return panel;
    }

    internal static GraphicsPath RoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        if (diameter <= 0) return path;
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    internal static string Initials(string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length == 0 ? "?" : string.Concat(words.TakeLast(2).Select(word => System.Globalization.StringInfo.GetNextTextElement(word))).ToUpperInvariant();
    }
}

// Native pages and Ctrl+Tab behavior; navigation is provided by accessible radio buttons.
internal sealed class PageTabs : TabControl
{
    public PageTabs() { Appearance = TabAppearance.FlatButtons; SizeMode = TabSizeMode.Fixed; ItemSize = new Size(1, 1); }
    public override Rectangle DisplayRectangle => ClientRectangle;
}

internal sealed class RoundedButton : Button
{
    private readonly bool _primary;
    private bool _hover;
    public RoundedButton(bool primary)
    {
        _primary = primary;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }
    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? Color.White);
        var bounds = new Rectangle(1, 1, Width - 3, Height - 3);
        using var path = UiTheme.RoundedPath(bounds, 9 * DeviceDpi / 96);
        var background = !Enabled ? Color.FromArgb(235, 238, 245) : _primary ? (_hover ? Color.FromArgb(53, 65, 181) : UiTheme.Accent) : (_hover ? UiTheme.AccentSoft : Color.White);
        using var fill = new SolidBrush(background);
        using var border = new Pen(_primary && Enabled ? background : UiTheme.Line);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
        TextRenderer.DrawText(e.Graphics, Text, Font, bounds, !Enabled ? UiTheme.Muted : _primary ? Color.White : UiTheme.Ink,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(bounds, -5, -5));
    }
}

// Keep native selection, keyboard navigation, tooltips and accessible item text.
internal sealed class ContactList : ListView
{
    private readonly ImageList _rowHeight = new() { ImageSize = new Size(1, 64) };
    public ContactList()
    {
        OwnerDraw = true;
        DoubleBuffered = true;
        SmallImageList = _rowHeight;
    }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); ScaleRows(); }
    protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); ScaleRows(); }
    private void ScaleRows() => _rowHeight.ImageSize = new Size(1, Math.Min(256, 64 * DeviceDpi / 96));
    protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e) { }
    protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e) { }
    protected override void OnDrawItem(DrawListViewItemEventArgs e)
    {
        var scale = DeviceDpi / 96f;
        int S(int value) => (int)(value * scale);
        var bounds = new Rectangle(2, e.Bounds.Top + S(3), ClientSize.Width - 5, e.Bounds.Height - S(6));
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = UiTheme.RoundedPath(bounds, S(10));
        using var fill = new SolidBrush(e.Item.Selected ? UiTheme.AccentSoft : Color.White);
        e.Graphics.FillPath(fill, path);
        var avatar = new Rectangle(bounds.Left + S(10), bounds.Top + (bounds.Height - S(36)) / 2, S(36), S(36));
        using var avatarFill = new SolidBrush(e.Item.Selected ? Color.FromArgb(215, 222, 255) : Color.FromArgb(237, 241, 248));
        e.Graphics.FillEllipse(avatarFill, avatar);
        TextRenderer.DrawText(e.Graphics, e.Item.Tag is GroupSummary ? "#" : UiTheme.Initials(e.Item.Text), Font, avatar, UiTheme.Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        var person = e.Item.Tag switch { FriendEntry friend => friend.User, UserSummary user => user, _ => null };
        if (person is not null)
        {
            var dot = new Rectangle(avatar.Right - S(9), avatar.Bottom - S(9), S(10), S(10));
            using var presence = new SolidBrush(person.IsOnline ? Color.FromArgb(22, 139, 100) : Color.FromArgb(155, 164, 181));
            using var outline = new Pen(Color.White, S(2));
            e.Graphics.FillEllipse(presence, dot);
            e.Graphics.DrawEllipse(outline, dot);
        }
        var textLeft = avatar.Right + S(10);
        var textWidth = Math.Max(10, bounds.Right - textLeft - S(10));
        var title = new Rectangle(textLeft, bounds.Top + S(9), textWidth, S(22));
        TextRenderer.DrawText(e.Graphics, e.Item.Text, Font, title, UiTheme.Ink, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        var details = e.Item.SubItems.Count >= 3 ? $"@{e.Item.SubItems[1].Text}  ·  {e.Item.SubItems[2].Text}"
            : e.Item.SubItems.Count == 2 ? e.Item.SubItems[1].Text : "Trò chuyện nhóm";
        using var small = new Font(Font.FontFamily, 8.5f);
        TextRenderer.DrawText(e.Graphics, details, small, new Rectangle(textLeft, bounds.Top + S(32), textWidth, S(20)), UiTheme.Muted, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        if (e.Item.Selected && Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(bounds, -2, -2));
    }
    protected override void Dispose(bool disposing) { if (disposing) _rowHeight.Dispose(); base.Dispose(disposing); }
}

internal sealed class RoundedPanel : Panel
{
    [System.ComponentModel.DefaultValue(typeof(Color), "White")]
    public Color FillColor { get; set; } = Color.White;
    public RoundedPanel() { DoubleBuffered = true; BackColor = Color.Transparent; }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = UiTheme.RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), 14 * DeviceDpi / 96);
        using var fill = new SolidBrush(FillColor);
        e.Graphics.FillPath(fill, path);
        base.OnPaint(e);
    }
}
