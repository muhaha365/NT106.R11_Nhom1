//Lưu trữ nội dung tin nhắn, đoạn hội thoại

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp_Client__gd2_3_.Models
{
    public class Message
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
