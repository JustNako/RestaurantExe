namespace Restaurant.Entities
{
    public class Ingredient
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = "g"; // единица измерения, например g, kg, ml, l, pcs
        public decimal CurrentQuantity { get; set; } // текущий остаток в единицах Unit
        public decimal MinimumQuantity { get; set; } // минимальный остаток для алертов

        public ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
    }
}






