using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Restaurant.Data;
using Restaurant.Entities;
using Restaurant.Hubs;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Restaurant.Services
{
    public class ReservationReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<NotificationsHub> _hubContext;

        public ReservationReminderService(IServiceProvider serviceProvider, IHubContext<NotificationsHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var now = DateTime.Now;
                var reminderTime = now.AddMinutes(15);

                // Найдем брони, начинающиеся примерно через 15 минут (±1 минута)
                var upcomingReservations = await context.Reservations
                    .Where(r => r.Status == ReservationStatus.Active &&
                                r.ReservationTime > now &&
                                r.ReservationTime <= reminderTime &&
                                !r.IsReminderSent) // Добавим поле для контроля (см. ниже)
                    .Include(r => r.Table)
                    .ToListAsync(stoppingToken);

                foreach (var reservation in upcomingReservations)
                {
                    var message = $"Бронирование за столом №{reservation.Table.Number} начинается через 15 минут: {reservation.ReservationTime:HH:mm}. Советуем не создавать новые заказы.";

                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", message, cancellationToken: stoppingToken);

                    // Помечаем, что уведомление отправлено, чтобы не спамить
                    reservation.IsReminderSent = true;
                }

                if (upcomingReservations.Any())
                    await context.SaveChangesAsync(stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
