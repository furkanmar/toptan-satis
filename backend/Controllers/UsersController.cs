using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WholesaleApi.Data;

namespace WholesaleApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(AppDbContext db) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Kendi Telegram chat ID'ini güncelle.
    /// Kullanıcı bota /start yazar, dönen chat_id'yi buraya kaydeder.
    /// </summary>
    [HttpPatch("me/telegram")]
    public async Task<IActionResult> UpdateTelegramChatId([FromBody] UpdateTelegramDto dto)
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user is null) return NotFound();

        user.TelegramChatId = string.IsNullOrWhiteSpace(dto.TelegramChatId)
            ? null
            : dto.TelegramChatId.Trim();

        await db.SaveChangesAsync();
        return Ok(new { user.Id, user.TelegramChatId });
    }

    /// <summary>
    /// Kendi bilgilerini getir (mevcut TelegramChatId dahil).
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user is null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.Role,
            user.TelegramChatId
        });
    }
}

public record UpdateTelegramDto(string? TelegramChatId);
