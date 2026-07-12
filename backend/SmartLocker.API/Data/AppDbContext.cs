using Microsoft.EntityFrameworkCore;
using SmartLocker.API.Models;

namespace SmartLocker.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Locker> Lockers => Set<Locker>();
    public DbSet<Command> Commands => Set<Command>();
    public DbSet<RentalHistory> RentalHistories => Set<RentalHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(50).IsRequired();
            e.Property(u => u.Password).HasMaxLength(256).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Locker>(e =>
        {
            e.Property(l => l.Name).HasMaxLength(50).IsRequired();
            e.Property(l => l.Status).HasMaxLength(20).HasDefaultValue("available");
        });

        modelBuilder.Entity<Command>(e =>
        {
            e.Property(c => c.Action).HasMaxLength(20).IsRequired();
            e.Property(c => c.Status).HasMaxLength(20).HasDefaultValue("pending");
            e.HasOne(c => c.Locker).WithMany(l => l.Commands).HasForeignKey(c => c.LockerId);
        });

        modelBuilder.Entity<RentalHistory>(e =>
        {
            e.Property(r => r.Status).HasMaxLength(20).HasDefaultValue("active");
            e.Property(r => r.PinCode).HasMaxLength(6).HasDefaultValue("");
            e.HasOne(r => r.User).WithMany(u => u.RentalHistories).HasForeignKey(r => r.UserId);
            e.HasOne(r => r.Locker).WithMany(l => l.RentalHistories).HasForeignKey(r => r.LockerId);
        });

        // Seed data: 2 lockers (giá trị cố định)
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Locker>().HasData(
            new Locker { Id = 1, Name = "Tủ 1", Status = "available", UpdatedAt = seedDate },
            new Locker { Id = 2, Name = "Tủ 2", Status = "available", UpdatedAt = seedDate }
        );
    }
}
