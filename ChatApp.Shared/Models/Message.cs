using System;
using System.Collections.Generic;
using System.Text;

namespace ChatApp.Shared.Models
{
    public class Message    //Lưu trữ nội dung tin nhắn, đoạn hội thoại
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int? ReceiverId { get; set; }
        public int? GroupId { get; set; }
        public string? Content { get; set; }
        public DateTime SendTime { get; set; }

        public Message()
        {
            SendTime = DateTime.Now;
        }
    }
}
