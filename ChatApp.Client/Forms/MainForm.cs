using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using ChatApp.Shared;

namespace ChatApp.Client.Forms
{
    public partial class MainForm : Form
    {

        public ClientConnection _connection = new();
        public MainForm()
        {

            InitializeComponent();
        }
        private bool _chatConnected = false;
        private async void btn_send_Click(object sender, EventArgs e)
        {
            _chatConnected = true;
            string message = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(message))
            {
                MessageBox.Show(this, "Vui lòng nhập tin nhắn!");
                return;
            }
            if (!_chatConnected)
            {
                MessageBox.Show(this, "Chưa kết nối đến server!");
                return;
            }
            Packet packet = new Packet
            {
                Type = "chat",
                Payload = JsonSerializer.SerializeToElement(message)
            };
            await _connection.SendAsync(packet);
            if (IsDisposed || Disposing) return;
            rtbChat.AppendText($"Bạn: {message}\n");
            txtMessage.Clear();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                await _connection.ConnectAsync("127.0.0.1", 5000);
                if (IsDisposed || Disposing) return;

                rtbChat.AppendText("Đã kết nối TCP tới server.\r\n");
                while(true)
                {
                    Packet? packet = await _connection.ReceiveAsync();
                    if(packet == null)
                    {
                        break;
                    }
                    if(packet.Type == "chat")
                    {
                        string? message = packet.Payload?.GetString();
                        rtbChat.AppendText($"{message}\n");
                    }
                }
                rtbChat.AppendText("Kết nối tới server đã đóng.\r\n");
            }
            catch (Exception ex)
            {
                if (IsDisposed || Disposing) return;
                MessageBox.Show(this, $"Không kết nối được: {ex.Message}");
                button1.Enabled = true;
                button1.Text = "Sign In";
            }
            finally
            {
                _connection.Disconnect();
                if (!IsDisposed && !Disposing)
                {
                    button1.Enabled = true;
                    button1.Text = "Sign In";
                }
            }
        }
    }
}
