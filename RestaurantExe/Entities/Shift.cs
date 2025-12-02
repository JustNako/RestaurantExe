namespace Restaurant.Entities
{
    public class Shift
    {
        public int Id { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public string OpenedBy { get; set; }
        public string? ClosedBy { get; set; }

        public string Role { get; set; } = string.Empty;

        public decimal OpeningCash { get; set; }
        public decimal? ClosingCash { get; set; }

        public List<Order> Orders { get; set; } = new();
    }

}

