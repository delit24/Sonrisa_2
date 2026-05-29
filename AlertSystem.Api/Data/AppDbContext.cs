using Microsoft.EntityFrameworkCore;
using AlertSystem.Api.Models.Entities;

namespace AlertSystem.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<AlertPreference> AlertPreferences => Set<AlertPreference>();
    public DbSet<NotificationChannel> NotificationChannels => Set<NotificationChannel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Role).HasDefaultValue("user");
            entity.Property(u => u.IsActive).HasDefaultValue(true);
        });

        // AlertPreference
        modelBuilder.Entity<AlertPreference>(entity =>
        {
            entity.HasIndex(p => new { p.UserId, p.Category, p.Keyword }).IsUnique();
            entity.HasIndex(p => p.UserId);
            entity.HasIndex(p => p.Category);

            entity.HasOne(p => p.User)
                  .WithMany(u => u.AlertPreferences)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // NotificationChannel
        modelBuilder.Entity<NotificationChannel>(entity =>
        {
            entity.HasIndex(c => new { c.UserId, c.Channel }).IsUnique();
            entity.HasIndex(c => c.UserId);

            entity.HasOne(c => c.User)
                  .WithMany(u => u.NotificationChannels)
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
