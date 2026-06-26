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

        cmd.Status = "done";
        cmd.ExecutedAt = DateTime.UtcNow;

        // Cập nhật tủ về available và hoàn thành lịch sử thuê
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

        await db.SaveChangesAsync();
        return (true, "Đã cập nhật trạng thái lệnh.");
    }
}
