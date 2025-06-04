// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.SignalR;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// //using Case2;
// [ApiController]
// [Route("api/messages")]
// public class MessagesController : ControllerBase
// {
    
//     [HttpGet]
//     public ActionResult<List<MessageDto>> GetMessages([FromQuery] int chatId)
//     {
//         var messages = DataStore.Messages
//             .Where(m => m.ChatId == chatId)
//             .Select(m => new MessageDto(m.Id, m.ChatId, m.SenderId, m.Text, m.SentAt, m.IsRead))
//             .ToList();
//         return Ok(messages);
//     }

//     [HttpPost]
//     public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, [FromServices] IHubContext<ChatHub> hubContext)
//     {
//         var message = new Message
//         {
//             Id = DataStore.GetNextMessageId(),
//             ChatId = request.ChatId,
//             SenderId = request.SenderId,
//             Text = request.Text,
//             SentAt = DateTime.UtcNow,
//             IsRead = false
//         };
//         DataStore.Messages.Add(message);

//         await hubContext.Clients.Group(request.ChatId.ToString())
//             .SendAsync("ReceiveMessage", request.ChatId, request.SenderId, request.Text);

//         return Ok(new { message = "Сообщение отправлено" });
//     }
// }
