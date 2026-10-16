using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp_Client__gd2_3_.Models
{
    public class Group
    {
        public int Id { get; set; }
        public string? GroupName { get; set; } 
        public int CreatedBy { get; set; } 
        public DateTime CreatedAt { get; set; }

        public Group()
        {
            CreatedAt = DateTime.Now;
        }
    }
}
