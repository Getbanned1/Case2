
using System;
namespace Case2
{
    public class User
    {
        required public int Id { get; set; }
        required public string Username { get; set; }
        required public string PasswordHash { get; set; }
        public int AvatarUrl { get; set; }
        public DateTime LastOnline { get; set; }
        public bool IsOnline { get; set; }
    }
}
