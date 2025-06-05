using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Case2;
[ApiController]
[Route("api/chats")]
public class ChatsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ChatsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<Chat>>> GetChats([FromQuery] int userId)
    {
        var chatIds = await _db.UserChats
            .Where(uc => uc.UserId == userId)
            .Select(uc => uc.ChatId)
            .ToListAsync();

        var chats = await _db.Chats
            .Where(c => chatIds.Contains(c.Id))
            .Select(c => new ChatDto(c.Id, c.Name, c.IsGroup))
            .ToListAsync();

        return Ok(chats);
    }

    [HttpPost("createChat")]
    public async Task<IActionResult> CreateChat([FromBody] CreateChatRequest request)
    {
        var chat = new Chat
        {
            Name = request.Name,
            IsGroup = request.IsGroup
        };
        _db.Chats.Add(chat);
        await _db.SaveChangesAsync();

        // Здесь нужно получить реальный userId создателя, например из токена

        _db.UserChats.Add(new UserChat { ChatId = chat.Id, UserId = request.creatorUserId });
        await _db.SaveChangesAsync();

        var chatDto = new ChatDto(chat.Id, chat.Name, chat.IsGroup);
        return CreatedAtAction(nameof(GetChats), new { userId = request.creatorUserId }, chatDto);
    }
}