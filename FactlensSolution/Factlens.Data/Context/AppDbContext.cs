using Factlens.Core.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Factlens.Data.Context
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<SearchRecord> SearchRecords { get; set; }
        public DbSet<FeedbackRecord> FeedbackRecords { get; set; }
        public DbSet<AiRequestLog> AiRequestLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>().HasIndex(u => u.UserName).IsUnique();
            builder.Entity<ApplicationUser>().HasIndex(u => u.Email).IsUnique();

            builder.Entity<SearchRecord>()
                .HasIndex(r => r.ShareId)
                .IsUnique();

            // ✅ SearchRecord → لو اليوزر اتمسح، UserId بتبقى null
            builder.Entity<SearchRecord>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // ✅ FeedbackRecord → SearchRecord: لو الـ SearchRecord اتمسح، الـ Feedback بتتمسح معاه
            builder.Entity<FeedbackRecord>()
                .HasOne(f => f.SearchRecord)
                .WithMany()
                .HasForeignKey(f => f.SearchRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ FeedbackRecord → لو اليوزر اتمسح، UserId بتبقى null
            builder.Entity<FeedbackRecord>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // ✅ AiRequestLog → لو اليوزر اتمسح، UserId بتبقى null
            builder.Entity<AiRequestLog>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}