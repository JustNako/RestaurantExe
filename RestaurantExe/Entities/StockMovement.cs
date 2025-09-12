namespace Restaurant.Entities
{
    public enum StockMovementType
    {
        Inbound = 1, // поступление на склад
        Outbound = 2 // списание со склада
    }

    public class StockMovement
    {
        public int Id { get; set; }
        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; }
        public decimal Quantity { get; set; } // положительное значение, единицы совпадают с Ingredient.Unit
        public StockMovementType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? Reason { get; set; } // комментарий, например "Закрытие заказа #123"
        public int? OrderId { get; set; }
        public Order? Order { get; set; }
    }
}






