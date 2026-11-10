using System.Net.Sockets;
using ChatApp.Server;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};

Console.WriteLine("Nhấn Ctrl+C để dừng server");
try
{
    string projectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID")
        ?? throw new InvalidOperationException("Chưa cấu hình FIREBASE_PROJECT_ID");
    var database = new Database(projectId);
    await database.CheckConnectionAsync(cancellation.Token);
    Console.WriteLine("Đã đọc/ghi Firestore thành công");
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.GetApplicationDefault(),
        ProjectId = projectId
    });
    ChatServer server = new ChatServer(database);
    await server.Run(5000, cancellation.Token);
}
catch(SocketException ex)
{
    Console.WriteLine($"Lỗi mạng: {ex.Message}");
}