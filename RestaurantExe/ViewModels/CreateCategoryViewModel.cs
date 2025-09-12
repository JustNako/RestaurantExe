using System.ComponentModel.DataAnnotations;

namespace Restaurant.ViewModels
{
    public class CreateCategoryViewModel
    {
        [Required]
        [Display(Name = "Category Name")]
        public string Name { get; set; }
    }

}
