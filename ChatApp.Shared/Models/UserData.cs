using System;
using System.Collections.Generic;
using System.Text;

namespace ChatApp.Shared.Models
{
    public class UserData   //Lưu thông tin đăng nhập tài khoản
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string DisplayName { get; set; }
        public bool IsOnline { get; set; }
        public DateTime CreatedAt { get; set; }

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
