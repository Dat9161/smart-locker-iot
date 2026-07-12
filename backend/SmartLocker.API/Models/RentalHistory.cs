namespace SmartLocker.API.Models;

public class RentalHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int LockerId { get; set; }
    public DateTime RentedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReturnedAt { get; set; }
    public string Status { get; set; } = "active"; // active | completed
    public string PinCode { get; set; } = string.Empty; // Mã PIN 4-6 số để mở tủ

    public User User { get; set; } = null!;
    public Locker Locker { get; set; } = null!;
}
