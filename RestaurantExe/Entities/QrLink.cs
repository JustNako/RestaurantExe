using System.ComponentModel.DataAnnotations;

namespace Restaurant.Entities
{
    public class QrLink
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;
        
        public QrLinkType Type { get; set; }
        
        public int? TableId { get; set; }
        public Table? Table { get; set; }
        
        [StringLength(500)]
        public string? CustomUrl { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime? ExpiresAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public int ScanCount { get; set; } = 0;
        
        public string? Description { get; set; }
    }
    
    public enum QrLinkType
    {
        Table,
        CustomUrl
    }
}


