namespace SmartLocker.API.Models;

public class RentalHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int LockerId { get; set; }
    public DateTime RentedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReturnedAt { get; set; }
    public string Status { get; set; } = "active"; // active | completed

    public User User { get; set; } = null!;
    public Locker Locker { get; set; } = null!;
}
