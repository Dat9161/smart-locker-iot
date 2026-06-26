namespace SmartLocker.API.DTOs;

public class RentalHistoryDto
{
    public int Id { get; set; }
    public string LockerName { get; set; } = string.Empty;
    public DateTime RentedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
