using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp_Client__gd2_3_.Models
{
    public class Friend
    {
        public int UserInvite {get; set;} 
        public int UserReceive {get; set;} 
        public string Status {get; set;} 
        public DateTime CreatedAt {get; set;}

        public Friend()
        {
            CreatedAt = DateTime.Now;
            Status = "Pending";
        }
    }
}
