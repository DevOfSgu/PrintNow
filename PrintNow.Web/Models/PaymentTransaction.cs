using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrintNow.Web.Models
{
    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(50)]
        public string TransactionType { get; set; } = string.Empty; // OrderPayment, PlatformFee

        public int? OrderId { get; set; }

        public int? PlatformFeeId { get; set; }

        public int? ShopId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(100)]
        public string PayOSOrderCode { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? PayOSPaymentLinkId { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed, Cancelled

        [MaxLength(500)]
        public string? PayOSResponse { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }
    }
}
