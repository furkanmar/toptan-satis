using WholesaleApi.Entities;

namespace WholesaleApi.Services;

public static class OrderStatusTransitions
{
    // Allowed transition matrix
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> Allowed = new()
    {
        [OrderStatus.Pending]   = [OrderStatus.Confirmed, OrderStatus.Rejected, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Delivered, OrderStatus.Cancelled],
        [OrderStatus.Delivered] = [OrderStatus.Confirmed],   // revert allowed via delivery note cancellation
        [OrderStatus.Rejected]  = [],   // terminal
        [OrderStatus.Cancelled] = [],   // terminal
    };

    public static bool CanTransitionTo(this OrderStatus current, OrderStatus next)
        => Allowed.TryGetValue(current, out var allowed) && allowed.Contains(next);
}
