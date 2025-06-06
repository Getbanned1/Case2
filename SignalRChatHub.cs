using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR.Client;
namespace Case2
{
    [Authorize]
    public class ChatHub : Hub
    {
        private HubConnection _hubConnection;
        private readonly AppDbContext _db;
        public ChatHub(AppDbContext db)
        {
            _db = db;
        }
        public async Task SendMessageViaSignalRAsync(int chatId, string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new HubException("Текст сообщения не может быть пустым.");

            // Создайте и сохраните сообщение в базе, если нужно
            // Отправьте сообщение всем клиентам в группе чата
            await Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", chatId, text);
        }

        public async Task<Chat> CreatePrivateChat(int userId1, int userId2)
        {
            var chat = new Chat { Name = $"Private_{userId1}_{userId2}", IsGroup = false };
            _db.Chats.Add(chat);
            await _db.SaveChangesAsync();

            _db.UserChats.AddRange(
                new UserChat { UserId = userId1, ChatId = chat.Id },
                new UserChat { UserId = userId2, ChatId = chat.Id }
            );
            await _db.SaveChangesAsync();

            return chat;
        }
        public async Task<List<MessageDto>> JoinRoom(int chatId)
        {
            var chat = await _db.Chats.FindAsync(chatId);
            if (chat == null)
                throw new HubException("Чат не найден.");

            await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());

           var messages = await _db.Messages
            .Where(m => m.ChatId == chatId)
            .Include(m => m.Sender)
            .OrderBy(m => m.SentAt)
            .Select(m => new MessageDto(
                m.Id,
                m.ChatId,
                m.SenderId,
                m.Text,
                m.SentAt,
                m.IsRead
            ))
            .ToListAsync();


            return messages;
        }


        public async Task LeaveRoom(int chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString());
        }
        public async Task SendMessage(int chatId, string text)
{
    try
    {
        // Получаем имя пользователя из Claims
        var username = Context.User?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            throw new HubException("Пользователь не найден.");

        // Создаём и сохраняем сущность сообщения
        var messageEntity = new Message
        {
            ChatId = chatId,
            SenderId = user.Id,
            Text = text,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };
        _db.Messages.Add(messageEntity);
        await _db.SaveChangesAsync();

        // Загружаем отправителя для возврата клиенту (если нужно)
        await _db.Entry(messageEntity).Reference(m => m.Sender).LoadAsync();

        // Формируем DTO для отправки клиентам
        var messageDto = new MessageDto(
            Id: messageEntity.Id,
            ChatId: messageEntity.ChatId,
            SenderId: messageEntity.SenderId,
            Text: messageEntity.Text,
            SentAt: messageEntity.SentAt,
            IsRead: messageEntity.IsRead
        );


        // Отправляем DTO всем участникам группы
        await Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", messageDto);
    }
    catch (Exception ex)
    {
        throw new HubException($"Ошибка при отправке сообщения: {ex.Message}");
    }
}

    }
}