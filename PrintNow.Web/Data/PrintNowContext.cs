using Microsoft.EntityFrameworkCore;
using PrintNow.Web.Models;

namespace PrintNow.Web.Data
{
    public class PrintNowContext : DbContext
    {
        public PrintNowContext(DbContextOptions<PrintNowContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Shop> Shops { get; set; }
        public DbSet<ShopOperatingHour> ShopOperatingHours { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<PlatformFee> PlatformFees { get; set; }
        public DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure some tricky relationships
            modelBuilder.Entity<Shop>()
                .HasOne(s => s.Owner)
                .WithMany(u => u.Shops)
                .HasForeignKey(s => s.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Customer)
                .WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Shop)
                .WithMany(s => s.Reviews)
                .HasForeignKey(r => r.ShopId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.Customer)
                .WithMany()
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Service)
                .WithMany()
                .HasForeignKey(od => od.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // PlatformFee relationship
            modelBuilder.Entity<PlatformFee>()
                .HasOne(pf => pf.Shop)
                .WithMany()
                .HasForeignKey(pf => pf.ShopId)
                .OnDelete(DeleteBehavior.Restrict);

            // WithdrawalRequest relationship
            modelBuilder.Entity<WithdrawalRequest>()
                .HasOne(wr => wr.Shop)
                .WithMany()
                .HasForeignKey(wr => wr.ShopId)
                .OnDelete(DeleteBehavior.Restrict);

            // PaymentTransaction - no FK constraints needed as it can reference multiple entities
        }
    }
}
