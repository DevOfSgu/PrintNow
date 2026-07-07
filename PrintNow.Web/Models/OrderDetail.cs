using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrintNow.Web.Models
{
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Order")]
        public int OrderId { get; set; }

        [ForeignKey("Service")]
        public int ServiceId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string FileUrl { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

        [MaxLength(20)]
        public string PaperSize { get; set; } = "A4";

        public bool IsColor { get; set; } = false;
        public int Sides { get; set; } = 1; // 1 or 2

        [MaxLength(500)]
        public string? AddOnList { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        public virtual Order Order { get; set; } = null!;
        public virtual Service Service { get; set; } = null!;
    }
}
