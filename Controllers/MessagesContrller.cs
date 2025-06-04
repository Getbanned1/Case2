using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Case2;
[ApiController]
[Route("api/messages")]
public class MessagesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessagesController(AppDbContext db, IHubContext<ChatHub> hubContext)
    {
        _db = db;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<MessageDto>>> GetMessages([FromQuery] int chatId)
    {
        var messages = await _db.Messages
            .Where(m => m.ChatId == chatId)
            .Select(m => new MessageDto(m.Id, m.ChatId, m.SenderId, m.Text, m.SentAt, m.IsRead))
            .ToListAsync();

        return Ok(messages);
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var message = new Message
        {
            ChatId = request.ChatId,
            SenderId = request.SenderId,
            Text = request.Text,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        await _hubContext.Clients.Group(request.ChatId.ToString())
            .SendAsync("ReceiveMessage", request.ChatId, request.SenderId, request.Text);

        return Ok(new { message = "Сообщение отправлено" });
    }
}