using ChatApp.Shared.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
// TUI VỪA THÊM 1: Thêm thư viện này để dùng được hàm băm mật khẩu
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static ChatApp.Client.AuthMessages;

namespace ChatApp.Client.Forms
{
    public partial class RegisterForm : Form
    {
        // TUI VỪA THÊM 2: Khai báo biến _connection để dùng cho việc kết nối TCP
        private readonly ClientConnection _connection = new();

        public RegisterForm()
        {
            InitializeComponent();
        }

        // TUI VỪA THÊM 3: Chèn hàm băm mật khẩu SHA256 vào đây
        private string HashSHA256(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        // TUI VỪA THÊM 4: Thêm chữ "async" vào ngay sau chữ "private" ở dòng này
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
                string idToken = completingProfile
                    ? await firebase.LoginAsync(email, password)
                    : await firebase.RegisterAsync(email, password);

                // Từ thời điểm đã nhận token, mọi lần thử tiếp phải đi đường đăng nhập.
                chkExistingAccount.Checked = true;
                await protocol.ConnectAsync("127.0.0.1", 5000);
                var result = await protocol.RequestAsync<AuthenticationResult>(
                    "auth.firebase.request",
                    new FirebaseLoginRequest(idToken, username, displayName));

                // Đóng phiên đăng ký để trở lại LoginForm; server giải phóng user trước khi trả lời.
                await protocol.RequestAsync<EmptyResult>("auth.logout.request", new { });
                MessageBox.Show(this, $"Đã hoàn tất hồ sơ {result.User.Username}. Bạn có thể đăng nhập.");
                Close();
            }
            catch (ProtocolRequestException ex) when (ex.Code == "PROFILE_REQUIRED")
            {
                MessageBox.Show(this,
                    "Tài khoản Firebase đã có. Mở Đăng ký, chọn Hoàn tất hồ sơ và nhập lại email/mật khẩu.");
            }
            catch (FirebaseSignInException ex)
            {
                MessageBox.Show(this, FirebaseErrorMessage(ex.Code));
            }
            catch (ProtocolRequestException ex)
            {
                MessageBox.Show(this, ex.Message);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(this, ex.Message);
            }
            catch (Exception)
            {
                MessageBox.Show(this, "Chưa hoàn tất đăng ký hoặc hồ sơ. Kiểm tra kết nối và thử lại.");
            }
            finally
            {
                if(!IsDisposed || Disposing)
                {
                    btnRegister.Enabled = true;
                    btnRegister.Text = "Sign Up";
                }
            }
        }
        private void txtPassword_TextChanged(object sender, EventArgs e)
        {

        }

        // TUI VỪA THÊM 5: Thêm hàm dọn dẹp bộ nhớ và ngắt TCP khi người dùng tắt Form này đi
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _connection.Dispose();
            base.OnFormClosed(e);
        }
    }
}