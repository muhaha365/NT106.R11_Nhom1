namespace ChatApp.Client.Forms
{
    partial class LoginForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btn_connect = new Button();
            panel1 = new Panel();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            txtUsername = new TextBox();
            label8 = new Label();
            label9 = new Label();
            label10 = new Label();
            label11 = new Label();
            txtPassword = new TextBox();
            label12 = new Label();
            label13 = new Label();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // btn_connect
            // 
            btn_connect.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            btn_connect.Location = new Point(545, 376);
            btn_connect.Margin = new Padding(3, 4, 3, 4);
            btn_connect.Name = "btn_connect";
            btn_connect.Size = new Size(267, 47);
            btn_connect.TabIndex = 0;
            btn_connect.Text = "Sign In";
            btn_connect.UseVisualStyleBackColor = true;
            btn_connect.Click += btn_connect_Click;
            // 
            // panel1
            // 
            panel1.BackColor = Color.Teal;
            panel1.Controls.Add(label7);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(label1);
            panel1.ForeColor = SystemColors.ControlLightLight;
            panel1.Location = new Point(-2, -3);
            panel1.Name = "panel1";
            panel1.Size = new Size(415, 538);
            panel1.TabIndex = 1;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 10F);
            label7.Location = new Point(29, 459);
            label7.Name = "label7";
            label7.Size = new Size(59, 19);
            label7.TabIndex = 6;
            label7.Text = "Nhóm 1";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 12F);
            label6.Location = new Point(104, 458);
            label6.Name = "label6";
            label6.Size = new Size(16, 21);
            label6.TabIndex = 5;
            label6.Text = "/";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 9F);
            label5.Location = new Point(122, 471);
            label5.Name = "label5";
            label5.Size = new Size(177, 15);
            label5.TabIndex = 4;
            label5.Text = "NT106 - Lập trình mạng căn bản";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            label4.ForeColor = Color.Azure;
            label4.Location = new Point(20, 271);
            label4.Name = "label4";
            label4.Size = new Size(129, 37);
            label4.TabIndex = 3;
            label4.Text = "Universe";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            label3.ForeColor = Color.Azure;
            label3.Location = new Point(16, 222);
            label3.Name = "label3";
            label3.Size = new Size(221, 37);
            label3.TabIndex = 2;
            label3.Text = "Communication";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            label2.ForeColor = Color.Azure;
            label2.Location = new Point(20, 169);
            label2.Name = "label2";
            label2.Size = new Size(134, 37);
            label2.TabIndex = 1;
            label2.Text = "Adaptive";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 30F, FontStyle.Bold);
            label1.ForeColor = Color.Azure;
            label1.Location = new Point(54, 51);
            label1.Name = "label1";
            label1.Size = new Size(191, 54);
            label1.TabIndex = 0;
            label1.Text = "NEXUS X";
            // 
            // txtUsername
            // 
            txtUsername.Location = new Point(583, 201);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(306, 23);
            txtUsername.TabIndex = 2;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new Font("Segoe UI", 30F, FontStyle.Bold);
            label8.ForeColor = Color.Black;
            label8.Location = new Point(478, 48);
            label8.Name = "label8";
            label8.Size = new Size(310, 54);
            label8.TabIndex = 7;
            label8.Text = "Welcome Back!";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Segoe UI", 10F);
            label9.ForeColor = Color.DarkGray;
            label9.Location = new Point(573, 124);
            label9.Name = "label9";
            label9.Size = new Size(253, 19);
            label9.TabIndex = 7;
            label9.Text = "Đăng nhập để gặp gỡ những người bạn";
            label9.TextAlign = ContentAlignment.TopRight;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label10.ForeColor = Color.DarkGray;
            label10.Location = new Point(436, 202);
            label10.Name = "label10";
            label10.Size = new Size(107, 19);
            label10.TabIndex = 8;
            label10.Text = "Tên đăng nhập";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label11.ForeColor = Color.DarkGray;
            label11.Location = new Point(478, 269);
            label11.Name = "label11";
            label11.Size = new Size(71, 19);
            label11.TabIndex = 10;
            label11.Text = "Mật khẩu";
            // 
            // txtPassword
            // 
            txtPassword.Location = new Point(583, 269);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new Size(306, 23);
            txtPassword.TabIndex = 9;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            label12.ForeColor = Color.DarkGray;
            label12.Location = new Point(559, 427);
            label12.Name = "label12";
            label12.Size = new Size(111, 15);
            label12.TabIndex = 11;
            label12.Text = "Chưa có tài khoản ?";
            label12.TextAlign = ContentAlignment.TopRight;
            label12.Click += label12_Click;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Font = new Font("Segoe UI", 9F, FontStyle.Underline);
            label13.ForeColor = Color.DeepSkyBlue;
            label13.Location = new Point(694, 427);
            label13.Name = "label13";
            label13.Size = new Size(64, 15);
            label13.TabIndex = 12;
            label13.Text = "Tạo tại đây";
            label13.TextAlign = ContentAlignment.TopRight;
            label13.Click += label13_Click;
            // 
            // LoginForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = Color.Honeydew;
            ClientSize = new Size(901, 521);
            Controls.Add(label13);
            Controls.Add(label12);
            Controls.Add(label11);
            Controls.Add(txtPassword);
            Controls.Add(label10);
            Controls.Add(label9);
            Controls.Add(label8);
            Controls.Add(txtUsername);
            Controls.Add(btn_connect);
            Controls.Add(panel1);
            Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            Margin = new Padding(3, 4, 3, 4);
            Name = "LoginForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = " ";
            Load += LoginForm_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btn_connect;
        private Panel panel1;
        private Label label1;
        private Label label2;
        private Label label7;
        private Label label6;
        private Label label5;
        private Label label4;
        private Label label3;
        private TextBox txtUsername;
        private Label label8;
        private Label label9;
        private Label label10;
        private Label label11;
        private TextBox txtPassword;
        private Label label12;
        private Label label13;
    }
}
