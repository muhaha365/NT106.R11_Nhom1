namespace ChatApp.Client
{
    partial class EmailVerificationForm
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
            lblEmail = new Label();
            lblInstruction = new Label();
            lblStatus = new Label();
            btnSendEmail = new Button();
            btnCheckVerification = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // lblEmail
            // 
            lblEmail.AutoSize = true;
            lblEmail.Location = new Point(50, 42);
            lblEmail.Name = "lblEmail";
            lblEmail.Size = new Size(110, 15);
            lblEmail.TabIndex = 0;
            lblEmail.Text = "Email cần xác minh";
            // 
            // lblInstruction
            // 
            lblInstruction.AutoSize = true;
            lblInstruction.Location = new Point(50, 83);
            lblInstruction.Name = "lblInstruction";
            lblInstruction.Size = new Size(396, 15);
            lblInstruction.TabIndex = 1;
            lblInstruction.Text = "Nhấn gửi thư, mở liên kết trong email, rồi quay lại bấm “Tôi đã xác minh”.";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(50, 198);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(190, 15);
            lblStatus.TabIndex = 2;
            lblStatus.Text = "Chưa kiểm tra trạng thái xác minh.";
            // 
            // btnSendEmail
            // 
            btnSendEmail.Location = new Point(50, 128);
            btnSendEmail.Name = "btnSendEmail";
            btnSendEmail.Size = new Size(123, 52);
            btnSendEmail.TabIndex = 3;
            btnSendEmail.Text = "Gửi thư xác minh";
            btnSendEmail.UseVisualStyleBackColor = true;
            // 
            // btnCheckVerification
            // 
            btnCheckVerification.Location = new Point(205, 128);
            btnCheckVerification.Name = "btnCheckVerification";
            btnCheckVerification.Size = new Size(123, 52);
            btnCheckVerification.TabIndex = 4;
            btnCheckVerification.Text = "Tôi đã xác minh";
            btnCheckVerification.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(359, 128);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(123, 52);
            btnCancel.TabIndex = 5;
            btnCancel.Text = "Để sau";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // EmailVerificationForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(544, 241);
            Controls.Add(btnCancel);
            Controls.Add(btnCheckVerification);
            Controls.Add(btnSendEmail);
            Controls.Add(lblStatus);
            Controls.Add(lblInstruction);
            Controls.Add(lblEmail);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "EmailVerificationForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Xác minh email";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblEmail;
        private Label lblInstruction;
        private Label lblStatus;
        private Button btnSendEmail;
        private Button btnCheckVerification;
        private Button btnCancel;
    }
}