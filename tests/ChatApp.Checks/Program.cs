namespace ChatApp.Checks;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            NetworkChecks.RunAsync().GetAwaiter().GetResult();
            Console.WriteLine("PASS: client request/response, events, errors, timeout, cancellation and disconnect.");
            if (!args.Contains("--network-only"))
            {
                string? images = args.Length >= 2 && args[0] == "--screenshots" ? args[1] : null;
                UiChecks.Run(images);
                Console.WriteLine("PASS: friend UI, pagination, invitations, group chat/history, dialogs, session guards and resize.");
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
