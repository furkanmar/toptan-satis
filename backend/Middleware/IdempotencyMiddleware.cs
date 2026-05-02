using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.Entities;

namespace WholesaleApi.Middleware;

/// <summary>
/// POST/PUT/PATCH isteklerinde Idempotency-Key header'ı varsa devreye girer.
/// Aynı key + aynı user → ilk response'u cache'ten döndürür, yan etki olmaz.
/// Concurrent aynı key isteklerinde DB unique constraint + retry ile ilk istek
/// tamamlanana kadar bekler (max 5 deneme, 200ms aralık).
/// </summary>
public class IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
{
    private static readonly HashSet<string> ApplicableMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH" };

    public async Task InvokeAsync(HttpContext ctx, IServiceScopeFactory scopeFactory)
    {
        // Sadece mutasyon metotları
        if (!ApplicableMethods.Contains(ctx.Request.Method))
        {
            await next(ctx);
            return;
        }

        // Header yoksa geç
        // Hem Idempotency-Key hem X-Idempotency-Key kabul edilir
        if (!ctx.Request.Headers.TryGetValue("X-Idempotency-Key", out var keyValues) &&
            !ctx.Request.Headers.TryGetValue("Idempotency-Key", out keyValues))
        {
            await next(ctx);
            return;
        }

        var key = keyValues.ToString().Trim();
        if (string.IsNullOrEmpty(key))
        {
            await next(ctx);
            return;
        }

        // Kullanıcı kimliği — anonim istekler atlanır
        var userIdStr = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            await next(ctx);
            return;
        }

        var path = ctx.Request.Path.Value ?? "";

        // Mevcut kayıt var mı kontrol et (retry loop — concurrent request senaryosu için)
        for (int attempt = 0; attempt < 5; attempt++)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var existing = await db.IdempotencyKeys
                .FirstOrDefaultAsync(ik =>
                    ik.Key == key &&
                    ik.UserId == userId &&
                    ik.ExpiresAt > DateTime.UtcNow);

            if (existing is not null)
            {
                // Cache hit — cached response'u dön
                logger.LogInformation(
                    "Idempotency cache hit: Key={Key} UserId={UserId} Path={Path} Status={Status}",
                    key, userId, path, existing.ResponseStatusCode);

                ctx.Response.StatusCode = existing.ResponseStatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(existing.ResponseBody);
                return;
            }

            // Response'u yakala
            var originalBody = ctx.Response.Body;
            using var buffer = new MemoryStream();
            ctx.Response.Body = buffer;

            try
            {
                await next(ctx);
            }
            catch
            {
                ctx.Response.Body = originalBody;
                throw;
            }

            buffer.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync();

            // Orijinal stream'e yaz
            buffer.Seek(0, SeekOrigin.Begin);
            ctx.Response.Body = originalBody;
            await buffer.CopyToAsync(originalBody);

            // Sadece başarılı (2xx) ya da iş mantığı hataları (4xx) cache'lenir.
            // 5xx geçici hata → cache'leme (tekrar denenebilir olsun).
            if (ctx.Response.StatusCode < 500)
            {
                try
                {
                    db.IdempotencyKeys.Add(new IdempotencyKey
                    {
                        Key = key,
                        UserId = userId,
                        RequestPath = path,
                        ResponseStatusCode = ctx.Response.StatusCode,
                        ResponseBody = responseBody,
                        ExpiresAt = DateTime.UtcNow.AddHours(24)
                    });
                    await db.SaveChangesAsync();

                    logger.LogInformation(
                        "Idempotency key kaydedildi: Key={Key} UserId={UserId} Status={Status}",
                        key, userId, ctx.Response.StatusCode);
                }
                catch (DbUpdateException ex) when (IsUniqueViolation(ex))
                {
                    // Concurrent istek aynı key'i kaydetti — bir sonraki loop'ta cache'ten dönecek
                    logger.LogWarning(
                        "Idempotency unique constraint ihlali (concurrent): Key={Key} Attempt={Attempt}",
                        key, attempt + 1);

                    if (attempt < 4)
                    {
                        await Task.Delay(200 * (attempt + 1));
                        continue;
                    }
                }
            }

            return;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("23505") == true ||      // PostgreSQL unique violation code
        ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true;
}
