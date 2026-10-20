using System;
using System.Collections.Generic;
using System.Text;

namespace ChatApp.Shared.Models
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
