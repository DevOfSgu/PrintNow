using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrintNow.Web.Models
{
    public class ShopOperatingHour
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Shop")]
        public int ShopId { get; set; }

        public int DayOfWeek { get; set; } // 0 = Sunday, 6 = Saturday

        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }

        public bool IsClosed { get; set; } = false;

        public virtual Shop Shop { get; set; } = null!;
    }
}
