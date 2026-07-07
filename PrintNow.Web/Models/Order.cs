using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrintNow.Web.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Customer")]
        public int CustomerId { get; set; }

        [ForeignKey("Shop")]
        public int ShopId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Printing, Ready, Completed, Cancelled

        [MaxLength(500)]
        public string? CancelReason { get; set; }

        [MaxLength(50)]
        public string PaymentStatus { get; set; } = "Unpaid"; // Unpaid, Paid, Refunded

        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Cash";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(50)]
        public string DeliveryMethod { get; set; } = "Pickup"; // Pickup, Delivery

        [MaxLength(500)]
        public string? ShippingAddress { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0m;

        public virtual User Customer { get; set; } = null!;
        public virtual Shop Shop { get; set; } = null!;
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
