using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrintNow.Web.Models
{
    public class PlatformFee
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Shop")]
        public int ShopId { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } = 199000m;

        [MaxLength(50)]
        public string Status { get; set; } = "Unpaid"; // Unpaid, Paid, Cancelled

        [MaxLength(100)]
        public string? PayOSOrderCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }

        // Navigation
        public virtual Shop Shop { get; set; } = null!;
    }
}
