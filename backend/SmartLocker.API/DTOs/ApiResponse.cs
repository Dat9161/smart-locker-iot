namespace SmartLocker.API.DTOs;

/// <summary>
/// Wrapper response chuẩn của toàn bộ API.
/// Thành công: { "success": true, "data": { ... } }
/// Thất bại:   { "success": false, "message": "..." }
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };

    public static ApiResponse<T> Fail(string message) => new() { Success = false, Message = message };
}
