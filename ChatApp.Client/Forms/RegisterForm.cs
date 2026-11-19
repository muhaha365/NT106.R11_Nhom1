using ChatApp.Shared.Contracts;
using System.Text;
using System.Text.RegularExpressions;
using static ChatApp.Client.AuthMessages;

namespace ChatApp.Client.Forms
{
    public partial class RegisterForm : Form
    {
        public RegisterForm()
        {
            InitializeComponent();
        }
        private async void button1_Click_1(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string username = txtUsername.Text.Trim().ToLowerInvariant();
            string displayName = txtDisplayName.Text.Trim();

            // Giữ nguyên mật khẩu, kể cả khoảng trắng.
            string password = txtPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;

            // Ghi nhận chế độ tại thời điểm nhấn nút.
            bool completingProfile = chkExistingAccount.Checked;

            if (string.IsNullOrWhiteSpace(email)
                || string.IsNullOrWhiteSpace(username)
                || string.IsNullOrWhiteSpace(displayName)
                || string.IsNullOrEmpty(password)
                || string.IsNullOrEmpty(confirmPassword))
            {
                MessageBox.Show(this, "Vui lòng điền đầy đủ thông tin.");
                return;
            }

            if (!Regex.IsMatch(username, @"\A[a-zA-Z0-9_]{3,20}\z"))
            {
                MessageBox.Show(this,
                    "Tên người dùng phải có 3–20 ký tự, chỉ gồm chữ, số và dấu gạch dưới.");
                return;
            }

            if (displayName.EnumerateRunes().Count() > 100
                || displayName.Any(char.IsControl))
            {
                MessageBox.Show(this,
                    "Tên hiển thị tối đa 100 ký tự và không chứa ký tự điều khiển.");
                return;
            }

            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                MessageBox.Show(this, "Mật khẩu nhập lại không khớp.");
                return;
            }

            btnRegister.Enabled = false;
            btnRegister.Text = "Đang xử lý...";

            try
            {
                var firebase = new FirebaseAuthClient(
                    Environment.GetEnvironmentVariable("FIREBASE_WEB_API_KEY") ?? "");
                using var protocol = new ProtocolClient();

                // Chỉ gọi signUp khi người dùng thực sự muốn tạo tài khoản mới.
                string idToken;
                if (completingProfile)
                {
                    idToken = await firebase.LoginAsync(email, password);
                }
                else
                {
                    idToken = await firebase.RegisterAsync(email, password);
                }

                // Từ thời điểm đã nhận token, mọi lần thử tiếp phải đi đường đăng nhập.
                chkExistingAccount.Checked = true;

                bool isVerified = await firebase.IsEmailVerifiedAsync(idToken);
                if(!isVerified)
                {
                    using var verification = new EmailVerificationForm(firebase, idToken, email);
                    var verificationResult = verification.ShowDialog(this);
                    if(verificationResult != DialogResult.OK)
                    {
                        return;
                    }
                    idToken = await firebase.LoginAsync(email, password);
                }

                await protocol.ConnectAsync("127.0.0.1", 5000);
                var result = await protocol.RequestAsync<AuthenticationResult>(
                    "auth.firebase.request",
                    new FirebaseLoginRequest(idToken, username, displayName));

                // Đóng phiên đăng ký để trở lại LoginForm; server giải phóng user trước khi trả lời.
                await protocol.RequestAsync<EmptyResult>("auth.logout.request", new { });
                MessageBox.Show(this, $"Đã hoàn tất hồ sơ {result.User.Username}. Bạn có thể đăng nhập.");
                Close();
            }
            catch (Exception error)
            {
                string message = AuthenticationErrorMessage(
                    error, "Chưa hoàn tất đăng ký hoặc hồ sơ. Kiểm tra kết nối và thử lại.");
                MessageBox.Show(this, message);
            }
            finally
            {
                if (!IsDisposed && !Disposing)
                {
                    btnRegister.Enabled = true;
                    btnRegister.Text = "Sign Up";
                }
            }
        }
    }
}
