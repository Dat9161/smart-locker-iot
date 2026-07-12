using SmartLocker.API.DTOs;

namespace SmartLocker.API.Services;

public interface ILockerService
{
    Task<List<LockerDto>> GetAllAsync();
    Task<(bool Success, string Message)> RentAsync(int lockerId, int userId, string pin);
    Task<(bool Success, string Message)> ReturnWithPinAsync(int lockerId, int userId, string pin);
    Task<List<RentalHistoryDto>> GetHistoryAsync(int userId);
}
