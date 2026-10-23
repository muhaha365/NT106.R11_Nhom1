using System.Security.Cryptography;
using ChatApp.Shared;
using System.Text;
using System.Text.Json;

namespace ChatApp.Client.Forms
{
    public partial class LoginForm : Form
    {
        private readonly ClientConnection _connection = new();
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

            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show(this, "Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu!");
                return;
            }

            btn_connect.Enabled = false;
            btn_connect.Text = "Đang kết nối...";

            try
            {
                await _connection.ConnectAsync("127.0.0.1", 5000);
                if (IsDisposed || Disposing) return;

                string passwordHash = HashSHA256(password);
                btn_connect.Text = "Đang xác thực...";

                btn_connect.Text = "Đã kết nối TCP";

                Packet packet = new Packet
                {
                    Type = "chat",
                    Payload = JsonSerializer.SerializeToElement("Xin chào từ client!")
                };

                await _connection.SendAsync(packet);
                await _connection.ReceiveLoopAsync();

                if (IsDisposed || Disposing) return;
                MessageBox.Show(this, "Kết nối tới server đã đóng");
            }

            catch (Exception ex)
            {
                if (IsDisposed || Disposing) return;
                MessageBox.Show(this, $"Không kết nối được: {ex.Message}");
                btn_connect.Enabled = true;
                btn_connect.Text = "Sign In";
            }
            finally
            {
                if (!IsDisposed && !Disposing)
                {
                    btn_connect.Enabled = true;
                    btn_connect.Text = "Sign In";
                }
            }
        }


        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _connection.Dispose();
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
    }
}
