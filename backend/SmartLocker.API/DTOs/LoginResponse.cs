namespace SmartLocker.API.DTOs;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int UserId { get; set; }
}
