namespace Restaurant.Entities
{
    public class RecipeItem
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; }
        public decimal QuantityPerUnit { get; set; } // сколько списывать ингредиента на 1 единицу блюда в Unit ингредиента
    }
}






