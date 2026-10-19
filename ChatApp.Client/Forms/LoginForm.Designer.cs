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
            SuspendLayout();
            // 
            // btn_connect
            // 
            btn_connect.Location = new Point(348, 252);
            btn_connect.Name = "btn_connect";
            btn_connect.Size = new Size(75, 23);
            btn_connect.TabIndex = 0;
            btn_connect.Text = "Kết nối";
            btn_connect.UseVisualStyleBackColor = true;
            btn_connect.Click += btn_connect_Click;
            // 
            // LoginForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btn_connect);
            Name = "LoginForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ChatApp - Đăng nhập";
            ResumeLayout(false);
        }

        #endregion

        private Button btn_connect;
    }
}
