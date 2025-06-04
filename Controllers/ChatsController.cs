 using Microsoft.AspNetCore.Mvc;
 using System.Collections.Generic;
 using System.Linq;
 using Case2;
 [ApiController]
 [Route("api/chats")]
 public class ChatsController : ControllerBase
 {
public record RegisterRequest(string Username, string Password);
public record LoginRequest(string Username, string Password);
public record CreateChatRequest(string Name, bool IsGroup);
public record SendMessageRequest(int ChatId, int SenderId, string Text);
    public static class DataStore
{
    public static List<User> Users = new List<User>();
    public static List<Chat> Chats = new List<Chat>();
    public static List<Message> Messages = new List<Message>();
    public static List<UserChat> UserChats = new List<UserChat>();

    private static int _userId = 1;
    private static int _chatId = 1;
    private static int _messageId = 1;

    public static int GetNextUserId() => _userId++;
    public static int GetNextChatId() => _chatId++;
    public static int GetNextMessageId() => _messageId++;
}
      //Получить чаты пользователя (по userId из query для примера)
     [HttpGet]
     public ActionResult<List<Chat>> GetChats([FromQuery] int userId)
     {
         var chatIds = DataStore.UserChats.Where(uc => uc.UserId == userId).Select(uc => uc.ChatId).ToList();
         var chats = DataStore.Chats.Where(c => chatIds.Contains(c.Id))
             .Select(c => new ChatDto(c.Id, c.Name, c.IsGroup))
             .ToList();
         return Ok(chats);
     }

     [HttpPost]
     public IActionResult CreateChat([FromBody] CreateChatRequest request)
     {
         var chat = new Chat
         {
             Id = DataStore.GetNextChatId(),
             Name = request.Name,
             IsGroup = request.IsGroup
         };
         DataStore.Chats.Add(chat);


         DataStore.UserChats.Add(new UserChat { ChatId = chat.Id, UserId = 1 });

         var chatDto = new ChatDto(chat.Id, chat.Name, chat.IsGroup);
         return CreatedAtAction(nameof(GetChats), new { userId = 1 }, chatDto);
     }
 }
