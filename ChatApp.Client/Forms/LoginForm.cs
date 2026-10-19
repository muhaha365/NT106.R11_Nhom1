namespace ChatApp.Client.Forms
{
    public partial class LoginForm : Form
    {
        private readonly ClientConnection _connection = new();
        public LoginForm()
        {
            InitializeComponent();
        }

        private async void btn_connect_Click(object sender, EventArgs e)
        {
            btn_connect.Enabled = false;
            try
            {
                await _connection.ConnectAsync("127.0.0.1", 5000);
                if (IsDisposed || Disposing) return;
                btn_connect.Text = "Đã kết nối TCP";
                MessageBox.Show(this, "Đã kết nối tới server.");
            }
            catch (Exception ex)
            {
                if (IsDisposed || Disposing) return;
                MessageBox.Show(this, $"Không kết nối được: {ex.Message}");
                btn_connect.Enabled = true;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _connection.Dispose();
            base.OnFormClosed(e);
        }
    }
}
