using WholesaleApi.DTOs;

namespace WholesaleApi.Services;

public interface IDeliveryNoteService
{
    Task<DeliveryNoteDto> CreateFromOrderAsync(Guid orderId, Guid wholesalerId, CreateDeliveryNoteDto dto);
    Task<DeliveryNoteDto> CancelAsync(Guid noteId, Guid wholesalerId, CancelDeliveryNoteDto dto);
    Task<DeliveryNoteDto> GetByIdAsync(Guid noteId, Guid wholesalerId);
    Task<List<DeliveryNoteListItemDto>> GetListAsync(Guid wholesalerId, Guid? storeId, DateTime? from, DateTime? to, string? status);
    Task<List<DeliveryNoteListItemDto>> GetForStoreAsync(Guid storeId, DateTime? from, DateTime? to, string? status);
}
