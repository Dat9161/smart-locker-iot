using Microsoft.EntityFrameworkCore;
using SmartLocker.API.Data;
using SmartLocker.API.DTOs;

namespace SmartLocker.API.Services;

public class CommandService(AppDbContext db) : ICommandService
{
    public async Task<CommandDto?> GetPendingCommandAsync(int lockerId)
    {
        var cmd = await db.Commands
            .Where(c => c.LockerId == lockerId && c.Status == "pending")
            .OrderBy(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (cmd == null) return null;

        return new CommandDto { Id = cmd.Id, LockerId = cmd.LockerId, Action = cmd.Action };
    }

    public async Task<(bool Success, string Message)> MarkDoneAsync(int commandId)
    {
        var cmd = await db.Commands.Include(c => c.Locker).FirstOrDefaultAsync(c => c.Id == commandId);
        if (cmd == null)
            return (false, "Không tìm thấy lệnh.");

        if (cmd.Status == "done")
            return (false, "Lệnh này đã được thực thi rồi.");

        cmd.Status = "done";
        cmd.ExecutedAt = DateTime.UtcNow;

        // Action "return" = lệnh trả tủ → set available + complete rental
        // Action "open"   = lệnh thuê tủ → tủ vẫn occupied sau khi đóng
        if (cmd.Action == "return")
        {
            cmd.Locker.Status = "available";
            cmd.Locker.UpdatedAt = DateTime.UtcNow;

            var rental = await db.RentalHistories
                .Where(r => r.LockerId == cmd.LockerId && r.Status == "active")
                .OrderByDescending(r => r.RentedAt)
                .FirstOrDefaultAsync();

            if (rental != null)
            {
                rental.Status = "completed";
                rental.ReturnedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        return (true, "Đã thực thi lệnh.");
    }
}
