
using System;
namespace Case2
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime LastOnline { get; set; }
        public bool IsOnline { get; set; }
        public ICollection<UserChat> UserChats { get; set; } = new List<UserChat>();
        public ICollection<Message> MessagesSent { get; set; } = new List<Message>();
    }
    public record UserDto(int Id, string Username);
}
