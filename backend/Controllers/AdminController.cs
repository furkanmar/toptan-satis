using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
using WholesaleApi.Entities;
using WholesaleApi.Services;
using WholesaleApi.Services.Storage;

namespace WholesaleApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController(
    AppDbContext db,
    IFileStorageService fileStorage,
    LocalFileStorage localStorage,
    IWebHostEnvironment env,
    IStockService stockService,
    IAuditService auditService) : ControllerBase
{
    // ─── Kullanıcı yönetimi ───────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await db.Users
            .Include(u => u.Wholesaler)
            .Include(u => u.Store)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.Email,
                Role = u.Role.ToString(),
                u.IsActive,
                u.CreatedAt,
                Wholesaler = u.Wholesaler == null ? null : new { u.Wholesaler.Id, u.Wholesaler.CompanyName },
                Store = u.Store == null ? null : new { u.Store.Id, u.Store.StoreName }
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPatch("users/{id:guid}")]
    public async Task<IActionResult> ToggleActive(Guid id, [FromBody] ToggleActiveDto dto)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        user.IsActive = dto.IsActive;
        await db.SaveChangesAsync();
        return Ok(new { user.Id, user.IsActive });
    }

    // ─── Magaza <-> Toptanci iliskileri ──────────────────────────────────────

    [HttpGet("store-wholesalers")]
    public async Task<IActionResult> GetStoreWholesalers()
    {
        var relations = await db.StoreWholesalers
            .Include(sw => sw.Store)
            .Include(sw => sw.Wholesaler)
            .OrderBy(sw => sw.Wholesaler.CompanyName)
            .ThenBy(sw => sw.Store.StoreName)
            .Select(sw => new
            {
                sw.StoreId,
                StoreName = sw.Store.StoreName,
                StorePhone = sw.Store.Phone,
                sw.WholesalerId,
                WholesalerName = sw.Wholesaler.CompanyName,
                sw.IsActive,
                sw.AssignedAt
            })
            .ToListAsync();

        return Ok(relations);
    }

    [HttpPost("store-wholesalers")]
    public async Task<IActionResult> CreateStoreWholesaler([FromBody] CreateStoreWholesalerDto dto)
    {
        var exists = await db.StoreWholesalers
            .AnyAsync(sw => sw.StoreId == dto.StoreId && sw.WholesalerId == dto.WholesalerId);
        if (exists) return BadRequest(new { error = "Bu iliski zaten mevcut" });

        var storeExists = await db.Stores.AnyAsync(s => s.Id == dto.StoreId);
        var wholesalerExists = await db.Wholesalers.AnyAsync(w => w.Id == dto.WholesalerId);
        if (!storeExists || !wholesalerExists) return NotFound(new { error = "Magaza veya toptanci bulunamadi" });

        db.StoreWholesalers.Add(new Entities.StoreWholesaler { StoreId = dto.StoreId, WholesalerId = dto.WholesalerId });
        await db.SaveChangesAsync();
        return Ok(new { message = "Iliski olusturuldu" });
    }

    [HttpDelete("store-wholesalers/{storeId:guid}/{wholesalerId:guid}")]
    public async Task<IActionResult> DeleteStoreWholesaler(Guid storeId, Guid wholesalerId)
    {
        var rel = await db.StoreWholesalers
            .FirstOrDefaultAsync(sw => sw.StoreId == storeId && sw.WholesalerId == wholesalerId);
        if (rel is null) return NotFound();

        db.StoreWholesalers.Remove(rel);
        await db.SaveChangesAsync();
        return Ok(new { message = "Iliski silindi" });
    }

    // ─── Audit log ────────────────────────────────────────────────────────────

    [HttpGet("audit-logs")]
    public async Task<AuditLogPageDto> GetAuditLogs(
        [FromQuery] Guid? userId,
        [FromQuery] string? entityType,
        [FromQuery] string? entityId,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        page = Math.Max(1, page);

        var q = db.AuditLogs.AsQueryable();

        if (userId.HasValue)           q = q.Where(a => a.UserId == userId);
        if (!string.IsNullOrEmpty(entityType)) q = q.Where(a => a.EntityType == entityType);
        if (!string.IsNullOrEmpty(entityId))   q = q.Where(a => a.EntityId == entityId);
        if (!string.IsNullOrEmpty(action))     q = q.Where(a => a.Action == action);
        if (from.HasValue) q = q.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue)   q = q.Where(a => a.CreatedAt <= to.Value);

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserRole = a.UserRole,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Changes = a.Changes,
                IpAddress = a.IpAddress,
                CreatedAt = a.CreatedAt,
            })
            .ToListAsync();

        return new AuditLogPageDto { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    // ─── Image Migration ──────────────────────────────────────────────────────
    // POST /api/admin/migrate-images-to-r2
    // Idempotent: "products/" ile baslayan key'ler skip edilir.

    [HttpPost("migrate-images-to-r2")]
    public async Task<IActionResult> MigrateImagesToR2()
    {
        var images = await db.ProductImages.ToListAsync();
        var total = images.Count;
        var migrated = 0;
        var skipped = 0;
        var failed = new List<string>();

        foreach (var image in images)
        {
            if (fileStorage.Owns(image.FilePath))
            {
                skipped++;
                continue;
            }

            if (!localStorage.Owns(image.FilePath))
            {
                failed.Add($"{image.Id}: unknown path format '{image.FilePath}'");
                continue;
            }

            try
            {
                await using var stream = await localStorage.DownloadAsync(image.FilePath);

                var ext = Path.GetExtension(image.FilePath);
                if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                var newKey = $"products/{image.ProductId}/{Guid.NewGuid()}{ext}";

                var contentType = ext.ToLower() switch
                {
                    ".webp" => "image/webp",
                    ".png"  => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    _ => "image/jpeg"
                };

                await fileStorage.UploadAsync(stream, newKey, contentType);

                var oldPath = image.FilePath;
                image.FilePath = newKey;
                await db.SaveChangesAsync();

                await localStorage.DeleteAsync(oldPath);
                migrated++;
            }
            catch (Exception ex)
            {
                failed.Add($"{image.Id} ({image.FilePath}): {ex.Message}");
            }
        }

        return Ok(new { total, migrated, skipped, failedCount = failed.Count, failed });
    }

    // ─── Manuel stok düzeltme ─────────────────────────────────────────────────
    // POST /api/admin/products/{id}/stock-adjustment

    [HttpPost("products/{id:guid}/stock-adjustment")]
    public async Task<IActionResult> StockAdjustment(Guid id, [FromBody] StockAdjustmentRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length < 10)
            return BadRequest(new { error = "Sebep en az 10 karakter olmalıdır" });

        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var delta = dto.NewStock - product.Stock;

        await stockService.ApplyMovementAsync(
            id, delta, MovementType.ManualAdjustment,
            orderId: null, userId, dto.Reason.Trim());

        auditService.LogAction(
            userId, "Admin", "ManualStockAdjustment", "Product", id.ToString(),
            new { OldStock = product.Stock, NewStock = dto.NewStock, Reason = dto.Reason.Trim() },
            HttpContext.Connection.RemoteIpAddress?.ToString());

        await db.SaveChangesAsync();
        return Ok(new { productId = id, newStock = product.Stock, delta });
    }
}

public record ToggleActiveDto(bool IsActive);
public record CreateStoreWholesalerDto(Guid StoreId, Guid WholesalerId);
