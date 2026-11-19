using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
namespace ChatApp.Client
{
    public partial class EmailVerificationForm : Form
    {
        private readonly FirebaseAuthClient _firebase;
        private readonly string _idToken;
        public EmailVerificationForm(FirebaseAuthClient firebase, string idToken, string email)
        {
            InitializeComponent();

            _firebase = firebase;
            _idToken = idToken;
            lblEmail.Text = $"Email: {email}";
        }
    }
}
