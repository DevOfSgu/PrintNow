using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrintNow.Web.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Order")]
        public int OrderId { get; set; }

        [ForeignKey("Shop")]
        public int ShopId { get; set; }

        [ForeignKey("Customer")]
        public int CustomerId { get; set; }

        [Range(1, 5)]
        public byte Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Order Order { get; set; } = null!;
        public virtual Shop Shop { get; set; } = null!;
        public virtual User Customer { get; set; } = null!;
    }
}
