namespace SmartLocker.API.DTOs;

public class CommandDto
{
    public int Id { get; set; }
    public int LockerId { get; set; }
    public string Action { get; set; } = string.Empty;
}
