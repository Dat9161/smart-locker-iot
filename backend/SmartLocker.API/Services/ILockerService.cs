using SmartLocker.API.DTOs;

namespace SmartLocker.API.Services;

public interface ILockerService
{
    Task<List<LockerDto>> GetAllAsync();
    Task<(bool Success, string Message)> RentAsync(int lockerId, int userId);
    Task<List<RentalHistoryDto>> GetHistoryAsync(int userId);
}
