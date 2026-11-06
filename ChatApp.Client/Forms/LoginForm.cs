using System.Security.Cryptography;
using ChatApp.Shared;
using System.Text;
using System.Text.Json;
using ChatApp.Shared.Contracts;
using static ChatApp.Client.AuthMessages;

namespace ChatApp.Client.Forms
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

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

        private async void btn_connect_Click(object sender, EventArgs e)
        {

            string username = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show(this, "Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu!");
                return;
            }

            ProtocolClient? protocol = new ProtocolClient();
            btn_connect.Enabled = false;
            try
            {
                var firebase = new FirebaseAuthClient(
                    Environment.GetEnvironmentVariable("FIREBASE_WEB_API_KEY") ?? "");
                string idToken = await firebase.LoginAsync(txtEmail.Text.Trim(), txtPassword.Text);
                await protocol.ConnectAsync("127.0.0.1", 5000);
                var result = await protocol.RequestAsync<AuthenticationResult>(
                    "auth.firebase.request", new FirebaseLoginRequest(idToken, null, null));

                using var main = new MainForm(protocol, result.User);
                protocol = null; // MainForm sở hữu kết nối từ đây, DisposeConnection đã có sẵn.
                Hide();
                try { main.ShowDialog(); }
                finally { if (!IsDisposed) Show(); }
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
                MessageBox.Show(this, "Chưa đăng nhập được. Kiểm tra kết nối và thử lại.");
            }
            finally
            {
                protocol?.Dispose();
                if (!IsDisposed) { btn_connect.Enabled = true; btn_connect.Text = "Sign In"; }
            }
        }


        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // _connection.Dispose();
            base.OnFormClosed(e);
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {

        }

        private void label13_Click(object sender, EventArgs e)
        {
            RegisterForm frmres = new RegisterForm();
            frmres.Show();
            frmres.FormClosed += (s, args) => this.Show();
            this.Hide();
        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void btn_demo_chat_Click(object sender, EventArgs e)
        {
            MainForm form = new MainForm();
            form.Show();
        }
    }
}
