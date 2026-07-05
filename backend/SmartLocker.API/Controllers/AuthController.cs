using Microsoft.AspNetCore.Mvc;
using SmartLocker.API.DTOs;
using SmartLocker.API.Services;

namespace SmartLocker.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        if (result == null)
            return Unauthorized(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không đúng." });

        return Ok(new { success = true, data = result });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (success, message) = await authService.RegisterAsync(request);
        if (!success)
            return BadRequest(new { success = false, message });

        return Ok(new { success = true, message });
    }
}
