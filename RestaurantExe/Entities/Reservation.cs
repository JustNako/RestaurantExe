using System;
using System.ComponentModel.DataAnnotations;

namespace Restaurant.Entities
{
    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        public int TableId { get; set; }
        public Table Table { get; set; }

        [Required]
        public DateTime ReservationTime { get; set; }

        public DateTime? EndTime { get; set; } // Время окончания бронирования (по умолчанию +2 часа)

        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(50)]
        public string CustomerPhone { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public ReservationStatus Status { get; set; } = ReservationStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? CreatedBy { get; set; } // ID пользователя, создавшего бронирование

        public bool IsReminderSent { get; set; } = false; // Флаг для отслеживания отправки напоминания

    }

    public enum ReservationStatus
    {
        Active,      // Активное бронирование
        Completed,   // Бронирование завершено (клиент пришел)
        Cancelled,   // Бронирование отменено
        NoShow       // Клиент не пришел
    }
}


