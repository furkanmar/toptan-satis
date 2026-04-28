using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
using WholesaleApi.Services;

namespace WholesaleApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(
    AppDbContext db,
    ITelegramNotificationService telegram) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user is null) return NotFound();
        return Ok(new { user.Id, user.Email, user.Role, user.TelegramChatId });
    }

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
    /// Telegram chat ID'sini test et: bota test mesajı gönder.
    /// </summary>
    [HttpPost("me/telegram/test")]
    public async Task<IActionResult> TestTelegram([FromBody] TestTelegramDto dto)
    {
        var chatId = dto.ChatId?.Trim();
        if (string.IsNullOrEmpty(chatId))
            return BadRequest(new { error = "Chat ID boş olamaz" });

        try
        {
            await telegram.SendMessageAsync(chatId,
                "✅ <b>Toptan Sipariş Sistemi</b>\n" +
                "Bu bir test mesajıdır. Bağlantınız başarıyla kuruldu!");
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Mesaj gönderilemedi: {ex.Message}" });
        }
    }

    /// <summary>
    /// Kendi bildirim geçmişini getir.
    /// </summary>
    [HttpGet("me/notifications")]
    public async Task<NotificationLogPageDto> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var q = db.NotificationLogs
            .Where(n => n.UserId == CurrentUserId)
            .OrderByDescending(n => n.CreatedAt);

        var total = await q.CountAsync();
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationLogDto
            {
                Id = n.Id,
                Type = n.Type.ToString(),
                Payload = n.Payload,
                Channel = n.Channel.ToString(),
                Status = n.Status.ToString(),
                AttemptCount = n.AttemptCount,
                LastAttemptAt = n.LastAttemptAt,
                ErrorMessage = n.ErrorMessage,
                CreatedAt = n.CreatedAt,
            })
            .ToListAsync();

        return new NotificationLogPageDto { Items = items, Total = total, Page = page, PageSize = pageSize };
    }
}

public record UpdateTelegramDto(string? TelegramChatId);
public record TestTelegramDto(string? ChatId);
