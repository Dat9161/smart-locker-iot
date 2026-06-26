using SmartLocker.API.DTOs;

namespace SmartLocker.API.Services;

public interface ICommandService
{
    Task<CommandDto?> GetPendingCommandAsync(int lockerId);
    Task<(bool Success, string Message)> MarkDoneAsync(int commandId);
}
