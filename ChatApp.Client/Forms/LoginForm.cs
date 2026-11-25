using ChatApp.Shared.Contracts;
using System.Security.Authentication;
using static ChatApp.Client.AuthMessages;

namespace ChatApp.Client.Forms
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }


        private async void btn_connect_Click(object sender, EventArgs e)
        {

            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show(this, "Vui lòng nhập email và mật khẩu.");
                return;
            }

            ProtocolClient? protocol = new ProtocolClient();
            btn_connect.Enabled = false;
            try
            {
                var firebase = new FirebaseAuthClient(Environment.GetEnvironmentVariable("FIREBASE_WEB_API_KEY") ?? "");
                string idToken = await firebase.LoginAsync(email, password);

                bool isVerified = await firebase.IsEmailVerifiedAsync(idToken);
                if (!isVerified)
                {
                    using var verification = new EmailVerificationForm(firebase, idToken, email);
                    var verificationResult = verification.ShowDialog(this);
                    if (verificationResult != DialogResult.OK)
                    {
                        return;
                    }
                    idToken = await firebase.LoginAsync(email, password);
                }
                await protocol.ConnectAsync("127.0.0.1", 5000);
                var result = await protocol.RequestAsync<AuthenticationResult>(
                    "auth.firebase.request", new FirebaseLoginRequest(idToken, null, null));

                using var main = new MainForm(protocol, result.User);
                protocol = null; // MainForm sở hữu kết nối từ đây, DisposeConnection đã có sẵn.
                Hide();
                try
                {
                    main.ShowDialog();
                }
                finally
                {
                    if (!IsDisposed)
                    {
                        Show();
                    }
                }
            }
            catch (Exception error)
            {
                string message = AuthenticationErrorMessage(
                    error, "Chưa đăng nhập được. Kiểm tra kết nối và thử lại.");
                MessageBox.Show(this, message);
            }
            finally
            {
                protocol?.Dispose();
                if (!IsDisposed)
                {
                    btn_connect.Enabled = true;
                    btn_connect.Text = "Sign In";
                }
            }
        }
        private async void btn_forgotpassword_Click(object sender, EventArgs e)
        {
            if (!btn_connect.Enabled || !btn_forgotpassword.Enabled)
            {
                return;
            }
            string email = txtEmail.Text.Trim();
            if(string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Vui lòng nhập email");
                return;
            }
            btn_connect.Enabled = false;
            btn_forgotpassword.Enabled = false;
            try
            {
                var firebase = new FirebaseAuthClient(Environment.GetEnvironmentVariable("FIREBASE_WEB_API_KEY") ?? "");
                await firebase.SendPasswordResetEmailAsync(email);
                
                if(IsDisposed || Disposing) return;

                MessageBox.Show(this, "Nếu email này có tài khoản phù hợp, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu. Hãy kiểm tra cả thư mục spam.");

            }
            catch (Exception error)
            {
                if (IsDisposed || Disposing) return;
                string message;
                if(error is FirebaseSignInException firebaseError && firebaseError.Code == "EMAIL_NOT_FOUND")
                {
                    message = "Nếu email này có tài khoản phù hợp, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu. Hãy kiểm tra cả thư mục spam.";
                }
                else
                {
                    message = AuthenticationErrorMessage(error, "Chưa gửi được yêu cầu đặt lại mật khẩu. Kiểm tra kết nối và thử lại.");
                }
                MessageBox.Show(this, message);
            }
            finally
            {
                if(!IsDisposed && !Disposing)
                {
                    btn_connect.Enabled = true;
                    btn_forgotpassword.Enabled = true;
                }
            }
        }

        private void label13_Click(object sender, EventArgs e)
        {
            RegisterForm frmres = new RegisterForm();
            frmres.Show();
            frmres.FormClosed += (s, args) => this.Show();
            this.Hide();
        }

        private void btn_demo_chat_Click(object sender, EventArgs e)
        {
            MainForm form = new MainForm();
            form.Show();
        }

    }
}
