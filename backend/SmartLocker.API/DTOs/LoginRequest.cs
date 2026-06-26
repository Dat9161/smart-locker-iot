using System.ComponentModel.DataAnnotations;

namespace SmartLocker.API.DTOs;

public class LoginRequest
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}
