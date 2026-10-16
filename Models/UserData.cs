//Lưu thông tin đăng nhập tài khoản

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp_Client__gd2_3_.Models
{
    public class UserData
    {
        public int Id {get; set;} 
        public string Username {get; set;} 
        public string PasswordHash {get; set;} 
        public string DisplayName {get; set;} 
        public bool IsOnline { get;set;} 
        public DateTime CreatedAt {get; set;}

        public UserData()
        {
            Username = string.Empty;
            PasswordHash = string.Empty;
            DisplayName = string.Empty;
            CreatedAt = DateTime.Now;
            IsOnline = false;
        }
    }
}
