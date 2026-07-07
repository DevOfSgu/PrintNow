using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrintNow.Web.Models
{
    public class Shop
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Owner")]
        public int OwnerId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ShopName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ tiệm in.")]
        public string AddressText { get; set; } = string.Empty;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? OperatingHoursText { get; set; } = "08:00 - 22:00";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual User Owner { get; set; } = null!;
        public virtual ICollection<ShopOperatingHour> OperatingHours { get; set; } = new List<ShopOperatingHour>();
        public virtual ICollection<Service> Services { get; set; } = new List<Service>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
