using Microsoft.EntityFrameworkCore;
using SmartLocker.API.Data;
using SmartLocker.API.DTOs;
using SmartLocker.API.Models;

namespace SmartLocker.API.Services;

public class LockerService(AppDbContext db) : ILockerService
{
    public async Task<List<LockerDto>> GetAllAsync()
    {
        return await db.Lockers
            .Select(l => new LockerDto { Id = l.Id, Name = l.Name, Status = l.Status })
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> RentAsync(int lockerId, int userId)
    {
        var locker = await db.Lockers.FindAsync(lockerId);
        if (locker == null)
            return (false, "Không tìm thấy tủ.");
        if (locker.Status != "available")
            return (false, "Tủ đang được sử dụng.");

        // Tạo command mở tủ
        var command = new Command { LockerId = lockerId, Action = "open", Status = "pending" };
        db.Commands.Add(command);

        // Cập nhật trạng thái tủ
        locker.Status = "occupied";
        locker.UpdatedAt = DateTime.UtcNow;

        // Ghi lịch sử thuê
        db.RentalHistories.Add(new RentalHistory { UserId = userId, LockerId = lockerId });

        await db.SaveChangesAsync();
        return (true, "Thuê tủ thành công.");
    }

    public async Task<List<RentalHistoryDto>> GetHistoryAsync(int userId)
    {
        return await db.RentalHistories
            .Where(r => r.UserId == userId)
            .Include(r => r.Locker)
            .OrderByDescending(r => r.RentedAt)
            .Select(r => new RentalHistoryDto
            {
                Id = r.Id,
                LockerName = r.Locker.Name,
                RentedAt = r.RentedAt,
                ReturnedAt = r.ReturnedAt,
                Status = r.Status
            })
            .ToListAsync();
    }
}
