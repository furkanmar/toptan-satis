using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.Entities;

namespace WholesaleApi.Services;

/// <summary>
/// Sipariş ve stok olaylarını ilgili kullanıcının Telegram'ına iletir.
/// Gönderim başarısız olursa NotificationLog'da kayıt kalır; iş akışı kesilmez.
/// </summary>
public class NotificationService(
    AppDbContext db,
    ITelegramNotificationService telegram,
    ILogger<NotificationService> logger)
{
    // ─── Order events ────────────────────────────────────────────────────────

    public async Task OrderCreatedAsync(Order order)
    {
        // Toptancıya bildir
        var wholesalerUserId = await db.Wholesalers
            .Where(w => w.Id == order.WholesalerId)
            .Select(w => w.UserId)
            .FirstOrDefaultAsync();

        var chatId = await GetChatIdAsync(wholesalerUserId);
        if (chatId is null) return;

        var itemCount = order.Items.Count;
        var storeName = order.Store?.StoreName ?? order.StoreId.ToString()[..8];
        var total = order.TotalAmount.ToString("N2");

        await telegram.SendMessageAsync(chatId,
            $"🛒 <b>Yeni Sipariş</b>\n" +
            $"Mağaza: <b>{storeName}</b>\n" +
            $"Kalem sayısı: {itemCount}\n" +
            $"Toplam: <b>{total} ₺</b>\n" +
            $"Sipariş ID: <code>{order.Id.ToString()[..8].ToUpper()}</code>");
    }

    public async Task OrderConfirmedAsync(Order order)
    {
        // Mağazaya bildir
        var storeUserId = await db.Stores
            .Where(s => s.Id == order.StoreId)
            .Select(s => s.UserId)
            .FirstOrDefaultAsync();

        var chatId = await GetChatIdAsync(storeUserId);
        if (chatId is null) return;

        var dueText = order.DueDate.HasValue
            ? order.DueDate.Value.ToString("dd.MM.yyyy")
            : "—";

        var note = string.IsNullOrEmpty(order.WholesalerNote)
            ? ""
            : $"\nNot: {order.WholesalerNote}";

        await telegram.SendMessageAsync(chatId,
            $"✅ <b>Siparişiniz Onaylandı</b>\n" +
            $"Sipariş ID: <code>{order.Id.ToString()[..8].ToUpper()}</code>\n" +
            $"Toplam: <b>{order.TotalAmount:N2} ₺</b>\n" +
            $"Vade tarihi: {dueText}" +
            note);
    }

    public async Task OrderRejectedAsync(Order order)
    {
        var storeUserId = await db.Stores
            .Where(s => s.Id == order.StoreId)
            .Select(s => s.UserId)
            .FirstOrDefaultAsync();

        var chatId = await GetChatIdAsync(storeUserId);
        if (chatId is null) return;

        var note = string.IsNullOrEmpty(order.WholesalerNote)
            ? ""
            : $"\nNot: {order.WholesalerNote}";

        await telegram.SendMessageAsync(chatId,
            $"❌ <b>Siparişiniz Reddedildi</b>\n" +
            $"Sipariş ID: <code>{order.Id.ToString()[..8].ToUpper()}</code>" +
            note);
    }

    public async Task OrderCancelledAsync(Order order, bool cancelledByWholesaler)
    {
        Guid targetUserId;

        if (cancelledByWholesaler)
        {
            // Toptancı iptal etti → mağazaya bildir
            targetUserId = await db.Stores
                .Where(s => s.Id == order.StoreId)
                .Select(s => s.UserId)
                .FirstOrDefaultAsync();
        }
        else
        {
            // Mağaza iptal etti → toptancıya bildir
            targetUserId = await db.Wholesalers
                .Where(w => w.Id == order.WholesalerId)
                .Select(w => w.UserId)
                .FirstOrDefaultAsync();
        }

        var chatId = await GetChatIdAsync(targetUserId);
        if (chatId is null) return;

        var who = cancelledByWholesaler ? "Toptancı" : "Mağaza";

        await telegram.SendMessageAsync(chatId,
            $"🚫 <b>Sipariş İptal Edildi</b>\n" +
            $"İptal eden: {who}\n" +
            $"Sipariş ID: <code>{order.Id.ToString()[..8].ToUpper()}</code>\n" +
            $"Toplam: {order.TotalAmount:N2} ₺");
    }

    // ─── Stock events ─────────────────────────────────────────────────────────

    public async Task LowStockAsync(Product product, int balanceAfter)
    {
        var wholesalerUserId = await db.Wholesalers
            .Where(w => w.Id == product.WholesalerId)
            .Select(w => w.UserId)
            .FirstOrDefaultAsync();

        var chatId = await GetChatIdAsync(wholesalerUserId);
        if (chatId is null) return;

        await telegram.SendMessageAsync(chatId,
            $"⚠️ <b>Düşük Stok Alarmı</b>\n" +
            $"Ürün: <b>{product.Name}</b>\n" +
            $"Mevcut stok: <b>{balanceAfter}</b>\n" +
            $"Minimum eşik: {product.MinimumStockLevel}");
    }

    // ─── Credit events ───────────────────────────────────────────────────────

    /// <summary>
    /// Vadesi geçen borç için toptancıya bildirim.
    /// OverdueAlertService tarafından sadece bir kez çağrılır (LastOverdueNotifiedAt koruması).
    /// </summary>
    public async Task OverdueDebitAsync(
        Guid wholesalerId,
        string storeName,
        decimal remainingAmount,
        DateTime dueDate)
    {
        var wholesalerUserId = await db.Wholesalers
            .Where(w => w.Id == wholesalerId)
            .Select(w => w.UserId)
            .FirstOrDefaultAsync();

        var chatId = await GetChatIdAsync(wholesalerUserId);
        if (chatId is null) return;

        await telegram.SendMessageAsync(chatId,
            $"⏰ <b>Vadesi Geçen Alacak</b>\n" +
            $"Mağaza: <b>{storeName}</b>\n" +
            $"Kalan tutar: <b>{remainingAmount:N2} ₺</b>\n" +
            $"Vade tarihi: {dueDate:dd.MM.yyyy}");
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private async Task<string?> GetChatIdAsync(Guid userId)
    {
        if (userId == Guid.Empty) return null;

        var chatId = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.TelegramChatId)
            .FirstOrDefaultAsync();

        if (chatId is null)
            logger.LogDebug(
                "TelegramChatId tanımlı değil: UserId={UserId} — bildirim atlandı", userId);

        return chatId;
    }
}
