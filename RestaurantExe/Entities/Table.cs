// Entities/Table.cs
using System.ComponentModel.DataAnnotations;

namespace Restaurant.Entities
{
    public class Table
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите номер стола")]
        public string Number { get; set; }

        public bool IsOccupied { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        // Навигация
        public ICollection<Order>? Orders { get; set; }
    }
}
