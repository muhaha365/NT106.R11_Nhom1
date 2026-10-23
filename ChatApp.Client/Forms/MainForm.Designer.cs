namespace ChatApp.Client.Forms
{
    partial class MainForm
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
            rtbChat = new RichTextBox();
            txtMessage = new TextBox();
            btn_send = new Button();
            button1 = new Button();
            SuspendLayout();
            // 
            // rtbChat
            // 
            rtbChat.Location = new Point(107, 29);
            rtbChat.Name = "rtbChat";
            rtbChat.Size = new Size(566, 260);
            rtbChat.TabIndex = 0;
            rtbChat.Text = "";
            // 
            // txtMessage
            // 
            txtMessage.Location = new Point(107, 324);
            txtMessage.Name = "txtMessage";
            txtMessage.Size = new Size(347, 23);
            txtMessage.TabIndex = 1;
            // 
            // btn_send
            // 
            btn_send.Location = new Point(501, 316);
            btn_send.Name = "btn_send";
            btn_send.Size = new Size(108, 36);
            btn_send.TabIndex = 2;
            btn_send.Text = "Send";
            btn_send.UseVisualStyleBackColor = true;
            btn_send.Click += btn_send_Click;
            // 
            // button1
            // 
            button1.Location = new Point(316, 381);
            button1.Name = "button1";
            button1.Size = new Size(118, 38);
            button1.TabIndex = 3;
            button1.Text = "Xác thực";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(button1);
            Controls.Add(btn_send);
            Controls.Add(txtMessage);
            Controls.Add(rtbChat);
            Name = "MainForm";
            Text = "MainForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private RichTextBox rtbChat;
        private TextBox txtMessage;
        private Button btn_send;
        private Button button1;
    }
}