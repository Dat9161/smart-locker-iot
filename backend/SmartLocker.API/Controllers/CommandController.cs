using Microsoft.AspNetCore.Mvc;
using SmartLocker.API.Services;

namespace SmartLocker.API.Controllers;

[ApiController]
[Route("api/command")]
public class CommandController(ICommandService commandService) : ControllerBase
{
    // ESP32 dùng endpoint này để polling lệnh chờ
    [HttpGet("pending/{lockerId:int}")]
    public async Task<IActionResult> GetPending(int lockerId)
    {
        var cmd = await commandService.GetPendingCommandAsync(lockerId);
        if (cmd == null)
            return NoContent(); // 204 - không có lệnh chờ

        return Ok(cmd);
    }

    // ESP32 gọi sau khi thực thi xong lệnh
    [HttpPost("done/{id:int}")]
    public async Task<IActionResult> MarkDone(int id)
    {
        var (success, message) = await commandService.MarkDoneAsync(id);
        if (!success)
            return NotFound(new { success = false, message });

        return Ok(new { success = true, message });
    }
}
