namespace SmartLocker.API.Models;

public class Locker
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g. "Tủ A1"
    public string Status { get; set; } = "available";  // available | occupied
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Command> Commands { get; set; } = [];
    public ICollection<RentalHistory> RentalHistories { get; set; } = [];
}
