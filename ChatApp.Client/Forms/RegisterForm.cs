using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
// TUI VỪA THÊM 1: Thêm thư viện này để dùng được hàm băm mật khẩu
using System.Security.Cryptography;

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
            string username = txtUsername.Text.Trim();
            string displayName = txtDisplayName.Text.Trim();
            string password = txtPassword.Text.Trim();
            string confirmPassword = txtConfirmPassword.Text.Trim();

            // 1. Kiểm tra rỗng
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(displayName) ||
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                MessageBox.Show(this, "Vui lòng điền đầy đủ tất cả các thông tin!");
                return;
            }
            if (!Regex.IsMatch(username, "^[a-zA-Z0-9]+$"))
            {
                MessageBox.Show(this, "Tên đăng nhập phải viết liền, không dấu và không chứa ký tự đặc biệt!");
                return;
            }

            // 2. Kiểm tra mật khẩu khớp nhau
            if (password != confirmPassword)
            {
                MessageBox.Show(this, "Mật khẩu nhập lại không khớp!");
                return;
            }

            btnRegister.Enabled = false;
            btnRegister.Text = "Đang xử lý...";

            try
            {
                // 3. Kết nối TCP tới Server
                await _connection.ConnectAsync("127.0.0.1", 5000);
                if (IsDisposed || Disposing) return;

                // 4. Băm mật khẩu để gửi đi an toàn
                string passwordHash = HashSHA256(password);

                btnRegister.Text = "Đang gửi yêu cầu đăng ký...";

                // Tới Giai đoạn 4, nhóm bạn sẽ tạo Packet JSON và gửi đi ở đây:
                // var registerPacket = new { Type = "REGISTER", Username = username, Password = passwordHash, DisplayName = displayName };
                // await _connection.SendAsync(registerPacket);
            }
            catch (Exception ex)
            {
                if (IsDisposed || Disposing) return;
                MessageBox.Show(this, $"Lỗi kết nối tới Server: {ex.Message}");
                btnRegister.Enabled = true;
                btnRegister.Text = "Đăng ký";
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