using System.ComponentModel.DataAnnotations;
using Restaurant.Entities;

namespace Restaurant.ViewModels
{
    public class QrLinkViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Введите код")]
        [StringLength(20, ErrorMessage = "Код не может быть длиннее 20 символов")]
        public string Code { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Выберите тип")]
        public QrLinkType Type { get; set; }
        
        public int? TableId { get; set; }
        
        [StringLength(500, ErrorMessage = "URL не может быть длиннее 500 символов")]
        public string? CustomUrl { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime? ExpiresAt { get; set; }
        
        [StringLength(200, ErrorMessage = "Описание не может быть длиннее 200 символов")]
        public string? Description { get; set; }
        
        // Для отображения
        public string? TableNumber { get; set; }
        public int ScanCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? QrCodeImageUrl { get; set; }
    }
}
