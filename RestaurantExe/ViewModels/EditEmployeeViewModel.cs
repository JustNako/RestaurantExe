using System.ComponentModel.DataAnnotations;

namespace Restaurant.ViewModels
{
    public class EditEmployeeViewModel
    {
        [Required]
        public string Id { get; set; }

        [Required(ErrorMessage = "Введите полное имя")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Введите номер телефона")]
        [Phone(ErrorMessage = "Введите корректный номер")]
        [RegularExpression(@"\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}", ErrorMessage = "Введите номер в формате +7 (___) ___-__-__")]
        public string PhoneNumber { get; set; }

        [DataType(DataType.Password)]
        public string? Password { get; set; } // необязательный

        [Required(ErrorMessage = "Выберите роль")]
        public string Role { get; set; }

        public IList<string> Roles { get; set; } = new List<string>();
    }
}
