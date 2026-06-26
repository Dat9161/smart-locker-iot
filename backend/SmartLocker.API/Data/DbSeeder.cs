using Microsoft.EntityFrameworkCore;
using SmartLocker.API.Models;

namespace SmartLocker.API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Chỉ seed nếu chưa có user nào
        if (await db.Users.AnyAsync()) return;

        db.Users.Add(new User
        {
            Username = "admin",
            Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
            FullName = "Quản trị viên",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
