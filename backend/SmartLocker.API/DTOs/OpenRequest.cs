using System.ComponentModel.DataAnnotations;

namespace SmartLocker.API.DTOs;

public class OpenRequest
{
    [Required] public int LockerId { get; set; }

    [Required]
    [RegularExpression(@"^\d{4,6}$", ErrorMessage = "PIN phải là 4-6 chữ số.")]
    public string Pin { get; set; } = string.Empty;
}
