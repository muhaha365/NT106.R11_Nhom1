using System.Text.Json;
using ChatApp.Client;
using ChatApp.Client.Forms;
using ChatApp.Shared;
using ChatApp.Shared.Contracts;

namespace ChatApp.Checks;

internal static class UiChecks
{
    public static void Run(string? imageDirectory)
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        using (var empty = Hidden(new MainForm()))
            RunWindow(empty, async () => {
                NetworkChecks.Check(!Find<Button>(empty, "createGroupButton").Enabled, "Create requires a session");
                NetworkChecks.Check(!Find<Button>(empty, "sendGroupButton").Enabled, "Send requires a session");
                NetworkChecks.Check(!Find<Button>(empty, "searchButton").Enabled, "Search requires a session");
                NetworkChecks.Check(Find<Label>(empty, "brandLabel").Height > 20, "Header remains visible at DPI scaling");
                Capture(empty, imageDirectory, "01-disconnected");
                await Task.CompletedTask;
            });

        using var client = new ProtocolClient(TimeSpan.FromSeconds(5));
        using var server = FixtureServer.ConnectAsync(client).GetAwaiter().GetResult();
        server.HoldNextHistory();
        using var window = Hidden(new MainForm(client, FixtureServer.Self));
        RunWindow(window, async () => {
            var friends = Find<ListView>(window, "friendsList");
            var pending = Find<ListView>(window, "pendingList");
            var groups = Find<ListView>(window, "groupsList");
            await Until(() => friends.Items.Count == 2 && pending.Items.Count == 2 && groups.Items.Count == 2);
            NetworkChecks.Check(server.Requests.Any(p => p.Type == "friend.list.request" && p.Payload!.Value.GetProperty("AfterId").ValueKind == JsonValueKind.Number), "Friend pagination");
            NetworkChecks.Check(server.Requests.Any(p => p.Type == "group.list.request" && p.Payload!.Value.GetProperty("AfterId").ValueKind == JsonValueKind.Number), "Group pagination");
            NetworkChecks.Check(friends.GetItemRect(0).Height >= friends.Font.Height * 3, "Avatar rows scale with screen DPI");
            var navigation = Find<FlowLayoutPanel>(window, "friendsTabsNavigation");
            foreach (var choice in navigation.Controls.OfType<RadioButton>())
                NetworkChecks.Check(choice.Width >= TextRenderer.MeasureText(choice.Text, choice.Font).Width + 12, "Navigation labels fit at screen DPI");
            NetworkChecks.Check(navigation.Controls.OfType<RadioButton>().Count(choice => choice.Checked) == 1, "Selected navigation item is visible");

            SelectGroup(groups, 10);
            await server.HeldHistoryAsync();
            await server.SendMessageEventAsync(FixtureServer.Message(102, 10, "Tin đến trong lúc tải lịch sử."));
            var messages = Find<Control>(window, "groupMessages");
            await Until(() => messages.Text.Contains("Tin đến trong lúc tải lịch sử."));
            await server.CompleteHistoryAsync();
            await Until(() => messages.Text.Contains("Chào nhóm"));
            NetworkChecks.Check(Occurrences(messages.Text, "Tin đến trong lúc tải lịch sử.") == 1, "History/event merge avoids duplicates");
            Find<Button>(window, "loadHistoryButton").PerformClick();
            await Until(() => messages.Text.Contains("Tin nhắn cũ"));
            NetworkChecks.Check(messages.Text.IndexOf("Tin nhắn cũ", StringComparison.Ordinal) < messages.Text.IndexOf("Chào nhóm", StringComparison.Ordinal), "Old history order");

            var box = Find<TextBox>(window, "groupMessageBox");
            box.Text = "Tin nhắn kiểm thử tiếng Việt.\r\nDòng thứ hai.";
            Find<Button>(window, "sendGroupButton").PerformClick();
            await Until(() => messages.Text.Contains("Tin nhắn kiểm thử") && box.Text.Length == 0);
            var send = server.Requests.Last(p => p.Type == "chat.send.request");
            NetworkChecks.Check(send.Payload!.Value.GetProperty("ReceiverId").ValueKind == JsonValueKind.Null && send.Payload.Value.GetProperty("GroupId").GetInt32() == 10, "Group-only routing");
            Capture(window, imageDirectory, "02-group-chat");
            var originalSize = window.Size;
            window.Size = window.MinimumSize;
            await Task.Delay(50);
            NetworkChecks.Check(box.Width > 100 && messages.Height > 80, "Minimum window layout");
            Capture(window, imageDirectory, "03-minimum-size");
            NetworkChecks.Check(!messages.Controls.OfType<Panel>().Single().HorizontalScroll.Visible, "Message bubbles do not overflow horizontally after resize");
            var longContent = string.Concat(Enumerable.Repeat("Tiếng Việt 🙂 ", 100)) + "\n" + new string('x', 500) + "\nDòng cuối";
            await server.SendMessageEventAsync(FixtureServer.Message(104, 10, longContent));
            await Until(() => messages.Text.Contains("Dòng cuối"));
            var viewport = messages.Controls.OfType<Panel>().Single();
            var rows = viewport.Controls.OfType<Panel>().ToArray();
            NetworkChecks.Check(rows.All(row => row.Right <= viewport.ClientSize.Width), "Bubble rows stay within the viewport");
            var bodies = rows.SelectMany(row => row.Controls.OfType<Panel>()).SelectMany(bubble => bubble.Controls.OfType<RichTextBox>()).ToArray();
            var longBody = bodies.Single(body => body.Text == longContent);
            NetworkChecks.Check(longBody.GetPositionFromCharIndex(longBody.TextLength).Y + longBody.Font.Height <= longBody.ClientSize.Height, "Wrapped Unicode and long unbroken text are not clipped inside a bubble");
            longBody.Select(0, "Tiếng Việt".Length);
            NetworkChecks.Check(longBody.ReadOnly && longBody.ShortcutsEnabled && longBody.SelectedText == "Tiếng Việt", "Message text retains native selection and copy shortcuts");
            longBody.Focus();
            longBody.Select(longBody.TextLength, 0);
            longBody.ScrollToCaret();
            await Until(() => {
                var caret = viewport.PointToClient(longBody.PointToScreen(longBody.GetPositionFromCharIndex(longBody.TextLength)));
                return caret.Y >= 0 && caret.Y + longBody.Font.Height <= viewport.Height;
            });
            longBody.Select(0, 0);
            await Until(() => {
                var caret = viewport.PointToClient(longBody.PointToScreen(longBody.GetPositionFromCharIndex(0)));
                return caret.Y >= 0 && caret.Y + longBody.Font.Height <= viewport.Height;
            });
            Find<TextBox>(window, "groupMessageBox").Focus();
            var scroll = messages.Controls.OfType<VScrollBar>().Single();
            scroll.Value = 0;
            NetworkChecks.Check(rows.Min(row => row.Top) >= 0, "Vertical scrollbar can reach the first message");
            scroll.Value = Math.Max(0, scroll.Maximum - scroll.LargeChange + 1);
            Capture(window, imageDirectory, "08-long-message");
            window.Size = originalSize;

            SelectGroup(groups, 20);
            await Until(() => messages.Text.Contains("nhóm bài lab"));
            await server.SendMessageEventAsync(FixtureServer.Message(103, 10, "Không hiện ở nhóm khác"));
            await Task.Delay(60);
            NetworkChecks.Check(!messages.Text.Contains("Không hiện ở nhóm khác"), "No cross-group event leakage");
            box.Text = "Bản nháp nhóm lab";
            server.HoldNextHistory();
            SelectGroup(groups, 10);
            await server.HeldHistoryAsync();
            SelectGroup(groups, 20);
            await server.CompleteHistoryAsync();
            await Until(() => messages.Text.Contains("nhóm bài lab"));
            NetworkChecks.Check(box.Text == "Bản nháp nhóm lab" && !messages.Text.Contains("Chào nhóm"), "Late old history ignored and draft preserved");

            var tabs = Find<TabControl>(window, "friendsTabs");
            navigation.Controls.OfType<RadioButton>().Single(choice => choice.Text == "Tìm người").PerformClick();
            NetworkChecks.Check(tabs.SelectedIndex == 2, "Navigation buttons switch the visible page");
            Find<TextBox>(window, "searchBox").Text = "ngoc";
            Find<Button>(window, "searchButton").PerformClick();
            var results = Find<ListView>(window, "searchResults");
            await Until(() => results.Items.Count == 1);
            results.Items[0].Selected = true;
            await Until(() => Find<Button>(window, "addFriendButton").Enabled);
            Capture(window, imageDirectory, "04-friend-search");
            Find<Button>(window, "addFriendButton").PerformClick();
            await Until(() => pending.Items.Count == 3);
            NetworkChecks.Check(!Find<Button>(window, "addFriendButton").Enabled, "Pending invite cannot be sent twice");
            tabs.SelectedIndex = 1;
            pending.Items.Cast<ListViewItem>().Single(row => ((FriendEntry)row.Tag!).User.Id == 5).Selected = true;
            NetworkChecks.Check(!Find<Button>(window, "acceptFriendButton").Enabled, "Cannot accept own outgoing invitation");
            pending.Items.Cast<ListViewItem>().Single(row => ((FriendEntry)row.Tag!).User.Id == 4).Selected = true;
            await Until(() => Find<Button>(window, "acceptFriendButton").Enabled);
            Find<Button>(window, "acceptFriendButton").PerformClick();
            await Until(() => friends.Items.Count == 3 && pending.Items.Count == 2);
            tabs.SelectedIndex = 0;

            using (var create = Hidden(new CreateGroupDialog(client, FixtureServer.Self, CancellationToken.None)))
            {
                create.Show(window);
                await Until(() => Find<Button>(create, "confirmCreateGroupButton").Enabled);
                Find<TextBox>(create, "groupNameBox").Text = "Nhóm nghiệm thu UI";
                Find<CheckedListBox>(create, "groupFriendChoices").SetItemChecked(0, true);
                Capture(create, imageDirectory, "05-create-group");
                Find<Button>(create, "confirmCreateGroupButton").PerformClick();
                await Until(() => create.IsDisposed || !create.Visible);
                NetworkChecks.Check(create.CreatedGroup?.Id == 30, "Create waits for server group result");
                await server.SendCreatedGroupAsync(create.CreatedGroup!);
            }
            await Until(() => groups.Items.Count == 3);
            SelectGroup(groups, 10);
            await Until(() => messages.Text.Contains("Chào nhóm"));
            using (var members = Hidden(new GroupMembersDialog(client, FixtureServer.FirstGroup, FixtureServer.Self, CancellationToken.None)))
            {
                members.Show(window);
                var list = Find<ListView>(members, "memberList");
                await Until(() => list.Items.Count == 3 && Find<Button>(members, "addMemberButton").Enabled);
                list.Items.Cast<ListViewItem>().Single(row => ((UserSummary)row.Tag!).Id == 1).Selected = true;
                NetworkChecks.Check(!Find<Button>(members, "removeMemberButton").Enabled, "Owner cannot remove self");
                Capture(members, imageDirectory, "06-members");
                Find<Button>(members, "addMemberButton").PerformClick();
                await Until(() => list.Items.Count == 4);
                NetworkChecks.Check(members.MembershipChanged, "Member addition acknowledged");
                members.Close();
            }
            using (var members = Hidden(new GroupMembersDialog(client, new(20, "Thảo luận bài lab", 2, DateTimeOffset.UtcNow), FixtureServer.Self, CancellationToken.None)))
            {
                members.Show(window);
                await Until(() => Find<ListView>(members, "memberList").Items.Count > 0);
                NetworkChecks.Check(!Find<Button>(members, "addMemberButton").Visible && !Find<Button>(members, "removeMemberButton").Visible, "Non-owner management actions hidden");
                members.Close();
            }

            await server.RemoveSelfAsync();
            await Until(() => groups.Items.Count == 2 && !Find<Button>(window, "sendGroupButton").Enabled);
            NetworkChecks.Check(messages.Text.Length == 0, "Removal clears selected group's history");
            client.Disconnect();
            await Until(() => friends.Items.Count == 0 && groups.Items.Count == 0);
            NetworkChecks.Check(Find<Label>(window, "currentUserLabel").Text == "Chưa đăng nhập", "Disconnect invalidates user context");
        });
        if (imageDirectory is not null)
            File.WriteAllLines(Path.Combine(imageDirectory, "ui-requests.jsonl"), server.Requests.Select(p => JsonSerializer.Serialize(p)));
        UnknownMutationResults(imageDirectory);
    }

    private static void UnknownMutationResults(string? imageDirectory)
    {
        using var client = new ProtocolClient(TimeSpan.FromMilliseconds(800));
        using var server = FixtureServer.ConnectAsync(client).GetAwaiter().GetResult();
        using var window = Hidden(new MainForm(client, FixtureServer.Self));
        RunWindow(window, async () => {
            var groups = Find<ListView>(window, "groupsList");
            await Until(() => groups.Items.Count == 2);
            SelectGroup(groups, 10);
            var history = Find<Control>(window, "groupMessages");
            await Until(() => history.Text.Contains("Chào nhóm"));
            server.DropNextReplyType = "chat.send.request";
            Find<TextBox>(window, "groupMessageBox").Text = "Tin đã lưu nhưng mất phản hồi";
            var send = Find<Button>(window, "sendGroupButton");
            send.PerformClick();
            var status = Find<Label>(window, "groupStatusLabel");
            await Until(() => status.Text.Contains("chưa xác định"));
            NetworkChecks.Check(!send.Enabled, "Uncertain send cannot immediately be repeated");
            send.PerformClick();
            NetworkChecks.Check(server.Requests.Count(p => p.Type == "chat.send.request") == 1, "No duplicate send after lost response");
            var sync = Find<Button>(window, "loadHistoryButton");
            NetworkChecks.Check(sync.Enabled && sync.Text.Contains("Đồng bộ"), "Latest history reconciliation is available");
            Capture(window, imageDirectory, "07-uncertain-result");
            sync.PerformClick();
            await Until(() => status.Text.Contains("Đã đồng bộ"));
            NetworkChecks.Check(history.Text.Contains("Tin đã lưu nhưng mất phản hồi"), "Reconciliation finds committed message");

            using var create = Hidden(new CreateGroupDialog(client, FixtureServer.Self, CancellationToken.None));
            create.Show(window);
            await Until(() => Find<Button>(create, "confirmCreateGroupButton").Enabled);
            Find<TextBox>(create, "groupNameBox").Text = "Nhóm đã tạo nhưng mất phản hồi";
            server.DropNextReplyType = "group.create.request";
            var confirm = Find<Button>(create, "confirmCreateGroupButton");
            confirm.PerformClick();
            await Until(() => create.NeedsRefresh);
            NetworkChecks.Check(!confirm.Enabled && create.CreatedGroup is null, "Uncertain group creation requires refresh, not assumed success");
            confirm.PerformClick();
            NetworkChecks.Check(server.Requests.Count(p => p.Type == "group.create.request") == 1, "No duplicate group creation after lost response");
            create.Close();
        });
    }

    private static T Hidden<T>(T form) where T : Form
    {
        form.Opacity = 0;
        form.ShowInTaskbar = false;
        return form;
    }

    private static void RunWindow(Form window, Func<Task> checks)
    {
        Exception? failure = null;
        window.Shown += async (_, _) => {
            try { await checks(); }
            catch (Exception error) { failure = error; }
            finally { window.Close(); }
        };
        Application.Run(window);
        if (failure is not null) throw new InvalidOperationException("UI verification failed.", failure);
    }

    private static T Find<T>(Control parent, string name) where T : Control => parent.Controls.Find(name, true).OfType<T>().Single();
    private static int Occurrences(string text, string term) => text.Split(term, StringSplitOptions.None).Length - 1;
    private static void SelectGroup(ListView list, int id)
    {
        foreach (ListViewItem row in list.Items) row.Selected = ((GroupSummary)row.Tag!).Id == id;
    }

    private static async Task Until(Func<bool> condition)
    {
        var end = DateTime.UtcNow.AddSeconds(6);
        while (!condition())
        {
            if (DateTime.UtcNow >= end) throw new TimeoutException("UI condition did not complete.");
            await Task.Delay(20);
        }
    }

    private static void Capture(Form form, string? directory, string name)
    {
        if (directory is null) return;
        Directory.CreateDirectory(directory);
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        bitmap.Save(Path.Combine(directory, name + ".png"));
    }
}
