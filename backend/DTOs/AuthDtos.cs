namespace WholesaleApi.DTOs;

public record LoginDto(string Email, string Password);

public record RegisterDto(string Email, string Password, string? CompanyName);

public class AuthResponseDto
{
    public string Token { get; set; } = null!;
    public string Role { get; set; } = null!;
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = null!;
}
