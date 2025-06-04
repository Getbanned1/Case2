using Microsoft.AspNetCore.SignalR;

namespace Case2
{
    public class ChatHub : Hub
    {
        // Отправка сообщения всем клиентам в группе чата
        public async Task SendMessage(Guid chatId, Guid senderId, string message)
        {
            // Здесь можно добавить сохранение сообщения в БД
            await Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", chatId, senderId, message);
        }

        public async Task JoinChat(Guid chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());
        }

        public async Task LeaveChat(Guid chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString());
        }
    }

}