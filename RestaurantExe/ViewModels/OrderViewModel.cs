using Restaurant.Entities;
using System.ComponentModel.DataAnnotations;

namespace Restaurant.ViewModels
{
    public class CreateOrderViewModel
    {
        public List<Category> Categories { get; set; } = new();
        public List<Table> Tables { get; set; } = new();
        public int? TableId { get; set; }
        public List<Product> Products { get; set; } = new();

        [Required(ErrorMessage = "Добавьте хотя бы один продукт.")]
        public List<OrderProductInput> ProductsWithQuantity { get; set; } = new();

        [Required(ErrorMessage = "Укажите, как будет выполнен заказ.")]
        public Place Place { get; set; }
    }

    public class OrderProductInput
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class EditOrderViewModel
    {
        public int Id { get; set; }
        public int? TableId { get; set; }
        public List<Table> Tables { get; set; } = new();

        [Required(ErrorMessage = "Укажите, как будет выполнен заказ.")]
        public Place Place { get; set; }
        public List<Category> Categories { get; set; } = new();
        public List<Product> Products { get; set; } = new();
        public List<OrderProductInput> ProductsWithQuantity { get; set; } = new();


    }





    public class OrdersIndexViewModel
    {
        public List<Order> Orders { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling((double)TotalCount / PageSize);
        public OrderStatus? SelectedStatus { get; set; }
        public int NewCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
    }
}
