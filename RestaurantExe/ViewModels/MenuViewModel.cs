using Restaurant.Entities;
using System.ComponentModel.DataAnnotations;

namespace RestaurantExe.ViewModels
{
    public class MenuViewModel
    {
        public string RestaurantName { get; set; }
        public List<Category> Categories { get; set; }
    }

}
