using System;
namespace Case2
{
    public class Message
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public int SenderId { get; set; }
        public string Text { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public Chat Chat { get; set; } = null!;
        public User Sender { get; set; } = null!;
    }

    public record MessageDto(int Id, int ChatId, int SenderId, string SenderAvatarUrl, string Text, DateTime SentAt, bool IsRead,string Sender);

    public record SendMessageRequest(int ChatId, int SenderId, string Text);
}
