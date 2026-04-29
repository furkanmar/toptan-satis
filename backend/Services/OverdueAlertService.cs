using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.Entities;

namespace WholesaleApi.Services;

/// <summary>
/// Günlük 09:00 UTC'de çalışan background service.
/// RemainingAmount > 0 ve DueDate geçmiş ve hiç bildirilmemiş borçlar için
/// toptancıya Telegram bildirimi atar. Spam önleme: LastOverdueNotifiedAt set edilir.
/// </summary>
public class OverdueAlertService(
    IServiceScopeFactory scopeFactory,
    ILogger<OverdueAlertService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OverdueAlertService başlatıldı");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now    = DateTime.UtcNow;
            var next9  = now.Date.AddHours(9);
            if (now >= next9) next9 = next9.AddDays(1);

            var delay = next9 - now;
            logger.LogInformation(
                "OverdueAlertService — sonraki çalışma: {Next} UTC ({Delay:hh\\:mm} sonra)",
                next9, delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (stoppingToken.IsCancellationRequested) break;

            await RunOverdueCheckAsync(stoppingToken);
        }

        logger.LogInformation("OverdueAlertService durduruldu");
    }

    private async Task RunOverdueCheckAsync(CancellationToken ct)
    {
        logger.LogInformation("OverdueAlertService — vade kontrolü başlıyor");

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db      = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notif   = scope.ServiceProvider.GetRequiredService<NotificationService>();

            var today = DateTime.UtcNow.Date;

            // Vadesi geçmiş, kapanmamış, hiç bildirilmemiş borçlar
            var overdueDebits = await db.CreditTransactions
                .Include(c => c.Store)
                .Where(c =>
                    (c.Type == CreditTransactionType.OrderDebit || c.Type == CreditTransactionType.ManualDebit) &&
                    c.DueDate.HasValue &&
                    c.DueDate.Value.Date < today &&
                    !c.IsFullyAllocated &&
                    c.LastOverdueNotifiedAt == null)
                .ToListAsync(ct);

            if (overdueDebits.Count == 0)
            {
                logger.LogInformation("OverdueAlertService — bildirilecek yeni vadesi geçmiş borç yok");
                return;
            }

            logger.LogInformation(
                "OverdueAlertService — {Count} borç için bildirim gönderiliyor", overdueDebits.Count);

            var now = DateTime.UtcNow;

            foreach (var debit in overdueDebits)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    await notif.OverdueDebitAsync(
                        debit.WholesalerId,
                        debit.Store.StoreName,
                        debit.Amount - debit.AllocatedAmount,
                        debit.DueDate!.Value);

                    debit.LastOverdueNotifiedAt = now;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "OverdueAlertService — bildirim gönderilemedi: DebitId={DebitId}", debit.Id);
                    // Tek başarısız bildirim tüm batch'i durdurmasın; devam et.
                }
            }

            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "OverdueAlertService — vade kontrolü tamamlandı, {Count} kayıt güncellendi",
                overdueDebits.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OverdueAlertService — RunOverdueCheckAsync exception");
        }
    }
}
