using System;

namespace Case2
{
    public class Chat
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsGroup { get; set; }
        public string? AvatarUrl { get; set; }
        public ICollection<UserChat> UserChats { get; set; } = new List<UserChat>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
}
    }
public record CreateChatRequest(string Name, bool IsGroup, int creatorUserId,string? AvatarUrl, List<int> ParticipantUserIds);

public record ChatDto(int Id, string Name, bool IsGroup, string AvatarUrl, bool isOnline,int? OtherUserId);
       
