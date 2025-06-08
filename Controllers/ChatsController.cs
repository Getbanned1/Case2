using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Case2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
[ApiController]
[Route("api/chats")]
public class ChatsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ChatHub> _chatHubContext;

    public ChatsController(AppDbContext db, IHubContext<ChatHub> chatHubContext)
    {
        _db = db;
        _chatHubContext = chatHubContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChatDto>>> GetChats([FromQuery] int userId)
    {
        var chatIds = await _db.UserChats
            .Where(uc => uc.UserId == userId)
            .Select(uc => uc.ChatId)
            .ToListAsync();

        var user = await _db.Users.FindAsync(userId);

        var chats = await _db.Chats
            .Where(c => chatIds.Contains(c.Id))
            .Select(c => new 
            {
                c.Id,
                c.Name,
                c.IsGroup,
                // Для группового чата берем аватар группы, если есть, иначе дефолт
                // Для приватного чата — аватар собеседника (участника, не равного userId)
                AvatarUrl = c.IsGroup 
                    ? (c.AvatarUrl ?? "https://localhost:7000/avatars/default_user.png") 
                    : c.UserChats.Where(uc => uc.UserId != userId).Select(uc => uc.User.AvatarUrl).FirstOrDefault() ?? "https://localhost:7000/avatars/default_user.png"
            })
            .ToListAsync();

        var chatDtos = chats.Select(c => new ChatDto(c.Id, c.Name, c.IsGroup, c.AvatarUrl)).ToList();

        return Ok(chatDtos);
    }



    [HttpPost("createPrivateChat")]
    public async Task<IActionResult> CreatePrivateChat([FromBody] CreateChatRequest request)
    {
        // Validate the request
        if (request.ParticipantUserIds == null || request.ParticipantUserIds.Count != 1)
        {
            return BadRequest("Exactly one participant user ID must be provided for a private chat.");
        }

        int userId1 = request.creatorUserId;
        int userId2 = request.ParticipantUserIds.First();

        // Check if both users exist
        var user1 = await _db.Users.FindAsync(userId1);
        var user2 = await _db.Users.FindAsync(userId2);

        if (user1 == null || user2 == null)
        {
            return NotFound("One or both users do not exist.");
        }

        var chat = _db.Chats.Add(new Chat { Name = user2.Username, IsGroup = false }).Entity; 
        await _db.SaveChangesAsync();
        try
        {
            await _chatHubContext.Clients.Users(userId1.ToString(), userId2.ToString()).SendAsync("CreatePrivateChat", new ChatDto(chat.Id, chat.Name, chat.IsGroup,user2.AvatarUrl));
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error creating chat: {ex.Message}");
        }

        // Optionally, you can emit a notification or perform additional actions here

        // Return the created chat details
        return Ok(new ChatDto(chat.Id, chat.Name, chat.IsGroup, user2.AvatarUrl));
    }
}

// Класс запроса

