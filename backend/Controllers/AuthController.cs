using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WholesaleApi.DTOs;
using WholesaleApi.Entities;
using WholesaleApi.Services;

namespace WholesaleApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<AuthResponseDto> Login([FromBody] LoginDto dto)
        => await authService.LoginAsync(dto);

    [HttpPost("register/wholesaler")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegisterWholesaler([FromBody] RegisterDto dto)
    {
        await authService.RegisterAsync(dto, UserRole.Wholesaler);
        return Ok(new { message = "Toptancı oluşturuldu" });
    }

    [HttpPost("register/store")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegisterStore([FromBody] RegisterDto dto)
    {
        await authService.RegisterAsync(dto, UserRole.Store);
        return Ok(new { message = "Mağaza oluşturuldu" });
    }
}
