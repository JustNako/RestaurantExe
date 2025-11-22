using System.ComponentModel.DataAnnotations;

namespace Restaurant.ViewModels
{
    public class CreateProductViewModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Image")]
        public IFormFile? ImageFile { get; set; }

        [Required]
        public int CategoryId { get; set; }
    }

}
