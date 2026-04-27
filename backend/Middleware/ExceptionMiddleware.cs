using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WholesaleApi.Exceptions;

namespace WholesaleApi.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DbUpdateConcurrencyException)
        {
            await WriteError(context, HttpStatusCode.Conflict,
                "Eş zamanlı güncelleme çakışması — lütfen isteği tekrarlayın");
        }
        catch (ForbiddenException ex)
        {
            await WriteError(context, HttpStatusCode.Forbidden, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteError(context, HttpStatusCode.Unauthorized, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteError(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteError(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            logger.LogError(ex, "Unhandled exception: {Type} — {Detail}", ex.GetType().Name, detail);
            await WriteError(context, HttpStatusCode.InternalServerError, $"Sunucu hatası: [{ex.GetType().Name}] {detail}");
        }
    }

    private static async Task WriteError(HttpContext ctx, HttpStatusCode code, string message)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = (int)code;
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }
}
