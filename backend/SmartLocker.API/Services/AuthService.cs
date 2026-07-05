using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartLocker.API.Data;
using SmartLocker.API.DTOs;

namespace SmartLocker.API.Services;

public class AuthService(AppDbContext db, IConfiguration config) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            return null;

        var token = GenerateJwt(user.Id, user.Username);
        return new LoginResponse { Token = token, FullName = user.FullName, UserId = user.Id };
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request)
    {
        var exists = await db.Users.AnyAsync(u => u.Username == request.Username);
        if (exists)
            return (false, "Tên đăng nhập đã tồn tại.");

        db.Users.Add(new Models.User
        {
            Username = request.Username,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return (true, "Đăng ký thành công.");
    }

    private string GenerateJwt(int userId, string username)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username)
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
