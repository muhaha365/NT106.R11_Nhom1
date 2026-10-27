namespace ChatApp.Client.Controls;

internal static class UiTheme
{
    public static readonly Color Accent = Color.FromArgb(0, 112, 112);
    public static readonly Color Surface = Color.FromArgb(244, 249, 248);
    public static readonly Color Muted = Color.FromArgb(75, 89, 91);

    public static Button CreateButton(string text, EventHandler? click = null, bool primary = false)
    {
        var button = new Button {
            Text = text, AutoSize = true, MinimumSize = new Size(0, 34), Padding = new Padding(8, 3, 8, 3),
            BackColor = primary ? Accent : Color.White, ForeColor = primary ? Color.White : Color.FromArgb(31, 49, 50),
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0),
            AccessibleName = text
        };
        button.FlatAppearance.BorderColor = primary ? Accent : Color.FromArgb(183, 201, 199);
        if (primary) button.EnabledChanged += (_, _) => {
            button.BackColor = button.Enabled ? Accent : Color.FromArgb(224, 233, 232);
            button.ForeColor = button.Enabled ? Color.White : Muted;
            button.FlatAppearance.BorderColor = button.Enabled ? Accent : Color.FromArgb(183, 201, 199);
        };
        if (click is not null) button.Click += click;
        return button;
    }

    public static Label CreateTitle(string text) => new() {
        Text = text, AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold),
        ForeColor = Color.FromArgb(26, 49, 49), Margin = new Padding(0, 0, 0, 8)
    };

    public static Label CreateHint(string text) => new() {
        Text = text, AutoSize = true, ForeColor = Muted, Margin = new Padding(0, 5, 0, 8),
        MaximumSize = new Size(520, 0), AccessibleName = text
    };

    public static ListView CreateList(params (string Title, int Width)[] columns)
    {
        var list = new ListView {
            Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false,
            HideSelection = false, ShowItemToolTips = true, HeaderStyle = ColumnHeaderStyle.Nonclickable, BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White, Font = new Font("Segoe UI", 10)
        };
        foreach (var column in columns) list.Columns.Add(column.Title, column.Width);
        return list;
    }

    public static FlowLayoutPanel CreateActions(params Control[] controls)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = new Padding(0, 8, 0, 0) };
        panel.Controls.AddRange(controls);
        return panel;
    }
}
