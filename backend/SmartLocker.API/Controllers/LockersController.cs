using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartLocker.API.DTOs;
using SmartLocker.API.Services;

namespace SmartLocker.API.Controllers;

[ApiController]
[Route("api/lockers")]
[Authorize]
public class LockersController(ILockerService lockerService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var lockers = await lockerService.GetAllAsync();
        return Ok(new { success = true, data = lockers });
    }

    [HttpPost("rent")]
    public async Task<IActionResult> Rent([FromBody] RentRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, message) = await lockerService.RentAsync(request.LockerId, userId);

        if (!success)
            return BadRequest(new { success = false, message });

        return Ok(new { success = true, message });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var history = await lockerService.GetHistoryAsync(userId);
        return Ok(new { success = true, data = history });
    }
}
