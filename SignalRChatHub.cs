using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Case2
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _db;
        public ChatHub(AppDbContext db)
        {
            _db = db;
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
        public async Task<List<Message>> JoinRoom(int chatId)
        {
            var chat = await _db.Chats.FindAsync(chatId);
            if (chat == null)
                throw new HubException("Чат не найден.");

            await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());

            var messages = await _db.Messages
                .Where(m => m.ChatId == chatId)
                .OrderByDescending(m => m.SentAt)
                .Include(m => m.Sender)
                .ToListAsync();

            return messages.OrderBy(m => m.SentAt).ToList();
        }

        public async Task LeaveRoom(int chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString());
        }
        public async Task SendMessage(int chatId, string text)
        {
            // Получаем имя пользователя из Claims
            var username = Context.User?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                throw new HubException("Пользователь не найден.");

            // Создаём и сохраняем сообщение
            var message = new Message
            {
                ChatId = chatId,
                SenderId = user.Id,
                Text = text,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };
            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            // Загружаем отправителя для возврата клиенту
            await _db.Entry(message).Reference(m => m.Sender).LoadAsync();

            // Отправляем сообщение всем участникам чата, кроме отправителя
            await Clients.GroupExcept(chatId.ToString(), new[] { Context.ConnectionId })
                .SendAsync("ReceiveMessage", message);
        }
    }
}