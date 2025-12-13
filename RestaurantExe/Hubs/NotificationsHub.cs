using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Restaurant.Hubs
{
    public class NotificationsHub : Hub
    {
        // Можно расширять, сейчас просто уведомления сервер → клиент
        public async Task SendNotification(string message)
        {
            await Clients.All.SendAsync("ReceiveNotification", message);
        }
    }
}
