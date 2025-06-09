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

        public override async Task OnConnectedAsync()
        {
            var userIdClaim = Context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                var userGroupName = $"User_{userIdClaim.Value}";
                await Groups.AddToGroupAsync(Context.ConnectionId, userGroupName);
            }
            await base.OnConnectedAsync();
        }

        // Метод для обновления статуса конкретного пользователя у его собеседников
        public async Task UpdateUserOnlineStatus(int userId, bool isOnline)
        {
            var chatIds = await _db.UserChats.Where(uc => uc.UserId == userId).Select(uc => uc.ChatId).ToListAsync();
            var otherUserIds = await _db.UserChats
                .Where(uc => chatIds.Contains(uc.ChatId) && uc.UserId != userId)
                .Select(uc => uc.UserId)
                .Distinct()
                .ToListAsync();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            user.IsOnline = isOnline;

            foreach (var otherUserId in otherUserIds)
            {
                await Clients.User(otherUserId.ToString()).SendAsync("ReceiveUserOnlineStatus", userId, isOnline);
            }
        }

        public async Task CreatePrivateChat(int userId1, int userId2, string Username, string chatName)
        {
            using (var transaction = await _db.Database.BeginTransactionAsync())
            {
                try
                {
                    var user1 = await _db.Users.FindAsync(userId1);
                    var user2 = await _db.Users.FindAsync(userId2);

                    if (user1 == null || user2 == null)
                    {
                        throw new HubException("One or both users do not exist.");
                    }

                    var existingChat = await _db.Chats
                        .Where(c => !c.IsGroup)
                        .Where(c => c.UserChats.Any(uc => uc.UserId == userId1))
                        .Where(c => c.UserChats.Any(uc => uc.UserId == userId2))
                        .FirstOrDefaultAsync();

                    if (existingChat != null)
                    {
                        throw new HubException("Chat between these users already exists.");
                    }

                    var chat = new Chat
                    {
                        Name = chatName,
                        IsGroup = false,
                    };

                    _db.Chats.Add(chat);
                    await _db.SaveChangesAsync();

                    var userChat1 = new UserChat { UserId = userId1, ChatId = chat.Id };
                    var userChat2 = new UserChat { UserId = userId2, ChatId = chat.Id };

                    _db.UserChats.AddRange(userChat1, userChat2);
                    await _db.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // Добавляем подключения пользователей в группу SignalR
                    var userGroupName1 = $"User_{userId1}";
                    var userGroupName2 = $"User_{userId2}";

                    // Предполагается, что в Hub при подключении пользователей вы добавляете их в группы User_{userId}
                    // Теперь отправляем сообщение о создании нового чата этим пользователям
                    await Clients.Groups(userGroupName1, userGroupName2)
                        .SendAsync("NewChatCreated", new
                        {
                            ChatId = chat.Id,
                            Name = chat.Name,
                            IsGroup = chat.IsGroup,
                            AvatarUrl = user2.AvatarUrl ?? string.Empty
                        });

                    // Опционально: можно отправить первое системное сообщение или другую информацию

                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
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
                 m.Sender.AvatarUrl ?? string.Empty,
                 m.Text,
                 m.SentAt,
                 m.IsRead,
                 m.Sender.Username
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
                var avatarUrl = user.AvatarUrl ?? string.Empty;
                // Формируем DTO для отправки клиентам
                var messageDto = new MessageDto(
                    Id: messageEntity.Id,
                    ChatId: messageEntity.ChatId,
                    SenderId: messageEntity.SenderId,
                    SenderAvatarUrl: avatarUrl,
                    Text: messageEntity.Text,
                    SentAt: messageEntity.SentAt,
                    IsRead: messageEntity.IsRead,
                    Sender: user.Username
                );


                // Отправляем DTO всем участникам группы
                await Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", messageDto);
            }
            catch (Exception ex)
            {
                throw new HubException($"Ошибка при отправке сообщения: {ex.Message}");
            }
        }
        public async Task MarkMessageAsRead(int messageId, int chatId)
        {
            var message = await _db.Messages.FindAsync(messageId);
            if (message == null) return;

            if (!message.IsRead)
            {
                message.IsRead = true;
                await _db.SaveChangesAsync();

                // Уведомляем всех участников чата, что сообщение прочитано
                await Clients.Group(chatId.ToString()).SendAsync("MessageRead", messageId);
            }
        }

        // public async Task MarkMessageAsDelivered(int messageId)
        // {
        //     var message = await _db.Messages.FindAsync(messageId);
        //     if (message == null) return;

        //     if (!message.IsDelivered)
        //     {
        //         message.IsDelivered = true;
        //         await _db.SaveChangesAsync();

        //         // Уведомляем отправителя, что сообщение доставлено
        //         await Clients.User(message.SenderId.ToString())
        //             .SendAsync("MessageDelivered", messageId);
        //     }
        // }
    }
}