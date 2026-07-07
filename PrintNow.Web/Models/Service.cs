using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrintNow.Web.Models
{
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Shop")]
        public int ShopId { get; set; }

        [MaxLength(50)]
        public string? ServiceCode { get; set; } // e.g., A4_BW, A3_COLOR, null for custom add-ons

        [Required]
        [MaxLength(100)]
        public string ServiceName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        [MaxLength(20)]
        public string Unit { get; set; } = "trang"; // trang, cuốn

        public bool IsActive { get; set; } = true;

        [MaxLength(20)]
        public string Type { get; set; } = "Core"; // Core, AddOn

        public virtual Shop Shop { get; set; } = null!;
    }
}
