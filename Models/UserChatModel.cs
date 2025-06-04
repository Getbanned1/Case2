using System;

namespace Case2
{
    public class UserChat
    {
        public int UserId { get; set; }
        public int ChatId { get; set; }
        public User User { get; set; } = null!;
        public Chat Chat { get; set; } = null!;
    }
}
