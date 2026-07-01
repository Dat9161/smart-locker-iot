namespace SmartLocker.API.Models;

public class Command
{
    public int Id { get; set; }
    public int LockerId { get; set; }
    public string Action { get; set; } = "open";    // open | close
    public string Status { get; set; } = "pending"; // pending | done
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExecutedAt { get; set; }

    public Locker Locker { get; set; } = null!;
}
