using SmartLocker.API.DTOs;

namespace SmartLocker.API.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request);
}
