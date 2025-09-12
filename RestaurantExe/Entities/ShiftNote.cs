using System.ComponentModel.DataAnnotations;

namespace Restaurant.Entities
{
    public class ShiftNote
    {
        public int Id { get; set; }
        public int ShiftId { get; set; }
        public Shift Shift { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

}
