namespace ChatApp.Client.Forms
{
    partial class RegisterForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panel1 = new Panel();
            btnRegister = new Button();
            panel2 = new Panel();
            label6 = new Label();
            txtEmail = new TextBox();
            label2 = new Label();
            txtConfirmPassword = new TextBox();
            label4 = new Label();
            txtDisplayName = new TextBox();
            label11 = new Label();
            txtPassword = new TextBox();
            label10 = new Label();
            txtUsername = new TextBox();
            label5 = new Label();
            label3 = new Label();
            label1 = new Label();
            chkExistingAccount = new CheckBox();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel1.BackColor = Color.Teal;
            panel1.Controls.Add(btnRegister);
            panel1.Controls.Add(panel2);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(label1);
            panel1.ForeColor = SystemColors.ControlLightLight;
            panel1.Location = new Point(31, 20);
            panel1.Margin = new Padding(3, 2, 3, 2);
            panel1.Name = "panel1";
            panel1.Size = new Size(940, 413);
            panel1.TabIndex = 2;
            // 
            // btnRegister
            // 
            btnRegister.Anchor = AnchorStyles.None;
            btnRegister.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            btnRegister.ForeColor = SystemColors.ControlText;
            btnRegister.Location = new Point(322, 367);
            btnRegister.Margin = new Padding(3, 2, 3, 2);
            btnRegister.Name = "btnRegister";
            btnRegister.Size = new Size(314, 44);
            btnRegister.TabIndex = 19;
            btnRegister.Text = "Sign Up";
            btnRegister.UseVisualStyleBackColor = true;
            btnRegister.Click += button1_Click_1;
            // 
            // panel2
            // 
            panel2.Anchor = AnchorStyles.None;
            panel2.Controls.Add(chkExistingAccount);
            panel2.Controls.Add(label6);
            panel2.Controls.Add(txtEmail);
            panel2.Controls.Add(label2);
            panel2.Controls.Add(txtConfirmPassword);
            panel2.Controls.Add(label4);
            panel2.Controls.Add(txtDisplayName);
            panel2.Controls.Add(label11);
            panel2.Controls.Add(txtPassword);
            panel2.Controls.Add(label10);
            panel2.Controls.Add(txtUsername);
            panel2.Location = new Point(144, 74);
            panel2.Margin = new Padding(3, 2, 3, 2);
            panel2.Name = "panel2";
            panel2.Size = new Size(662, 280);
            panel2.TabIndex = 5;
            panel2.Paint += panel2_Paint;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            label6.ForeColor = Color.DarkGray;
            label6.Location = new Point(178, 29);
            label6.Name = "label6";
            label6.Size = new Size(64, 28);
            label6.TabIndex = 29;
            label6.Text = "Email";
            // 
            // txtEmail
            // 
            txtEmail.Font = new Font("Segoe UI", 12F);
            txtEmail.Location = new Point(285, 29);
            txtEmail.Margin = new Padding(3, 2, 3, 2);
            txtEmail.Name = "txtEmail";
            txtEmail.Size = new Size(268, 29);
            txtEmail.TabIndex = 28;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            label2.ForeColor = Color.DarkGray;
            label2.Location = new Point(63, 212);
            label2.Name = "label2";
            label2.Size = new Size(194, 28);
            label2.TabIndex = 27;
            label2.Text = "Xác nhận mật khẩu";
            // 
            // txtConfirmPassword
            // 
            txtConfirmPassword.Font = new Font("Segoe UI", 12F);
            txtConfirmPassword.Location = new Point(285, 213);
            txtConfirmPassword.Margin = new Padding(3, 2, 3, 2);
            txtConfirmPassword.Name = "txtConfirmPassword";
            txtConfirmPassword.Size = new Size(268, 29);
            txtConfirmPassword.TabIndex = 26;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            label4.ForeColor = Color.DarkGray;
            label4.Location = new Point(109, 116);
            label4.Name = "label4";
            label4.Size = new Size(152, 28);
            label4.TabIndex = 25;
            label4.Text = "Tên đăng nhập";
            // 
            // txtDisplayName
            // 
            txtDisplayName.Font = new Font("Segoe UI", 12F);
            txtDisplayName.Location = new Point(285, 71);
            txtDisplayName.Margin = new Padding(3, 2, 3, 2);
            txtDisplayName.Name = "txtDisplayName";
            txtDisplayName.Size = new Size(268, 29);
            txtDisplayName.TabIndex = 24;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            label11.ForeColor = Color.DarkGray;
            label11.Location = new Point(161, 164);
            label11.Name = "label11";
            label11.Size = new Size(102, 28);
            label11.TabIndex = 23;
            label11.Text = "Mật khẩu";
            // 
            // txtPassword
            // 
            txtPassword.Font = new Font("Segoe UI", 12F);
            txtPassword.Location = new Point(285, 166);
            txtPassword.Margin = new Padding(3, 2, 3, 2);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new Size(268, 29);
            txtPassword.TabIndex = 22;
            txtPassword.TextChanged += txtPassword_TextChanged;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            label10.ForeColor = Color.DarkGray;
            label10.Location = new Point(158, 70);
            label10.Name = "label10";
            label10.Size = new Size(104, 28);
            label10.TabIndex = 21;
            label10.Text = "Họ và tên";
            // 
            // txtUsername
            // 
            txtUsername.Font = new Font("Segoe UI", 12F);
            txtUsername.Location = new Point(285, 118);
            txtUsername.Margin = new Padding(3, 2, 3, 2);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(268, 29);
            txtUsername.TabIndex = 20;
            // 
            // label5
            // 
            label5.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 9F);
            label5.Location = new Point(683, 390);
            label5.Name = "label5";
            label5.Size = new Size(225, 15);
            label5.TabIndex = 4;
            label5.Text = "Nhóm 1/NT106 - Lập trình mạng căn bản";
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            label3.ForeColor = Color.Azure;
            label3.Location = new Point(15, 381);
            label3.Name = "label3";
            label3.Size = new Size(85, 25);
            label3.TabIndex = 2;
            label3.Text = "Nexus X";
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top;
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 30F, FontStyle.Bold);
            label1.ForeColor = Color.Azure;
            label1.Location = new Point(288, 18);
            label1.Name = "label1";
            label1.Size = new Size(366, 54);
            label1.TabIndex = 0;
            label1.Text = "Đăng ký tài khoản";
            // 
            // chkExistingAccount
            // 
            chkExistingAccount.AutoSize = true;
            chkExistingAccount.Location = new Point(245, 247);
            chkExistingAccount.Name = "chkExistingAccount";
            chkExistingAccount.Size = new Size(265, 19);
            chkExistingAccount.TabIndex = 30;
            chkExistingAccount.Text = "Tôi đã có tài khoản Firebase — hoàn tất hồ sơ";
            chkExistingAccount.UseVisualStyleBackColor = true;
            // 
            // RegisterForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(999, 449);
            Controls.Add(panel1);
            Margin = new Padding(3, 2, 3, 2);
            Name = "RegisterForm";
            Text = "RegisterForm";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Label label5;
        private Label label3;
        private Label label1;
        private Button btnRegister;
        private Panel panel2;
        private Label label6;
        private TextBox txtEmail;
        private Label label2;
        private TextBox txtConfirmPassword;
        private Label label4;
        private TextBox txtDisplayName;
        private Label label11;
        private TextBox txtPassword;
        private Label label10;
        private TextBox txtUsername;
        private CheckBox chkExistingAccount;
    }
}