using System.ComponentModel.DataAnnotations;

namespace SmartLocker.API.DTOs;

public class RentRequest
{
    [Required] public int LockerId { get; set; }
}
