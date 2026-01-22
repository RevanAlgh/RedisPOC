using Microsoft.EntityFrameworkCore;
using RedisPOC.Models;

namespace RedisPOC.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var e = modelBuilder.Entity<UserProfile>();

            e.ToTable("UserProfiles");
            e.HasKey(x => x.Id);

            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.UpdatedAtUtc).IsRequired();
        }
    }
}
