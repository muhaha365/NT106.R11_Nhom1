using System.Net.Sockets;
using ChatApp.Server;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};

Console.WriteLine("Nhấn Ctrl+C để dừng server");
try
{
    ChatServer server = new ChatServer();
    await server.Run(5000);
}
catch(SocketException ex)
{
    Console.WriteLine($"Lỗi mạng: {ex.Message}");
}