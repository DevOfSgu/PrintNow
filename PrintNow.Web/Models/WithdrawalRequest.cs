using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrintNow.Web.Models
{
    public class WithdrawalRequest
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Shop")]
        public int ShopId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [MaxLength(100)]
        public string BankAccountName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string BankAccountNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string BankName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? AdminNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }

        // Navigation
        public virtual Shop Shop { get; set; } = null!;
    }
}
