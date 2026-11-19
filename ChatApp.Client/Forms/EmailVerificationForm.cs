using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
namespace ChatApp.Client.Forms
{
    public partial class EmailVerificationForm : Form
    {
        private readonly FirebaseAuthClient _firebase;
        private readonly string _idToken;
        private bool _busy;
        public EmailVerificationForm(FirebaseAuthClient firebase, string idToken, string email)
        {
            InitializeComponent();

            _firebase = firebase;
            _idToken = idToken;
            lblEmail.Text = $"Email: {email}";
        }
        private void SetBusy(bool busy)
        {
            _busy = busy;
            btnSendEmail.Enabled = !busy;
            btnCheckVerification.Enabled = !busy;
        }

        private async void btnSendEmail_Click(object sender, EventArgs e)
        {
            if(_busy) return;
            SetBusy(true);
            lblStatus.Text = "Đang gửi email xác thực...";

            try
            {
                await _firebase.SendVerificationEmailAsync(_idToken);
                if(IsDisposed || Disposing) return;
                lblStatus.Text = "Đã gửi yêu cầu thành công. Hãy kiểm tra hộp thư và thư rác.";
                btnSendEmail.Text = "Gửi lại email xác thực";
            }
            catch (Exception error)
            {
                if(IsDisposed || Disposing) return;
                lblStatus.Text = AuthMessages.AuthenticationErrorMessage(error, "Chưa gửi được email. Kiểm tra kết nối và thử lại.");
            }
            finally
            {
                if(!IsDisposed && !Disposing)
                {
                    SetBusy(false);
                }
            }
        }
    }
}
