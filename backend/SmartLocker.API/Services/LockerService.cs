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

    // Thuê tủ: lưu PIN + tạo lệnh open ngay → servo mở
    public async Task<(bool Success, string Message)> RentAsync(int lockerId, int userId, string pin)
    {
        var locker = await db.Lockers.FindAsync(lockerId);
        if (locker == null)
            return (false, "Không tìm thấy tủ.");
        if (locker.Status != "available")
            return (false, "Tủ đang được sử dụng.");

        // Lưu PIN vào lịch sử thuê
        db.RentalHistories.Add(new RentalHistory
        {
            UserId   = userId,
            LockerId = lockerId,
            PinCode  = pin,
            Status   = "active"
        });

        // Tạo lệnh mở tủ ngay cho ESP32
        db.Commands.Add(new Command { LockerId = lockerId, Action = "open", Status = "pending" });

        locker.Status    = "occupied";
        locker.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return (true, "Thuê tủ thành công! Tủ đang mở.");
    }

    // Trả tủ: yêu cầu đúng PIN → tạo lệnh return → servo mở để lấy đồ
    public async Task<(bool Success, string Message)> ReturnWithPinAsync(int lockerId, int userId, string pin)
    {
        var locker = await db.Lockers.FindAsync(lockerId);
        if (locker == null)
            return (false, "Không tìm thấy tủ.");
        if (locker.Status != "occupied")
            return (false, "Tủ này chưa được thuê.");

        // Xác minh PIN của lần thuê đang active
        var rental = await db.RentalHistories
            .Where(r => r.LockerId == lockerId && r.UserId == userId && r.Status == "active")
            .OrderByDescending(r => r.RentedAt)
            .FirstOrDefaultAsync();

        if (rental == null)
            return (false, "Bạn chưa thuê tủ này.");

        if (rental.PinCode != pin)
            return (false, "PIN không đúng.");

        // PIN đúng → tạo lệnh return cho ESP32
        db.Commands.Add(new Command { LockerId = lockerId, Action = "return", Status = "pending" });
        await db.SaveChangesAsync();

        return (true, "PIN đúng! Tủ đang mở, vui lòng lấy đồ ra.");
    }

    public async Task<List<RentalHistoryDto>> GetHistoryAsync(int userId)
    {
        return await db.RentalHistories
            .Where(r => r.UserId == userId)
            .Include(r => r.Locker)
            .OrderByDescending(r => r.RentedAt)
            .Select(r => new RentalHistoryDto
            {
                Id         = r.Id,
                LockerName = r.Locker.Name,
                RentedAt   = r.RentedAt,
                ReturnedAt = r.ReturnedAt,
                Status     = r.Status
            })
            .ToListAsync();
    }
}
