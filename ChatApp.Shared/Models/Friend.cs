using System;
using System.Collections.Generic;
using System.Text;

namespace ChatApp.Shared.Models
{
    public class Friend
    {
        public int UserInvite { get; set; }
        public int UserReceive { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public Friend()
        {
            CreatedAt = DateTime.Now;
            Status = "Pending";
        }
    }


}
