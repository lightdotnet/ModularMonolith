using StarterKit.Orders.Contracts.Common;

namespace StarterKit.Orders.Contracts.Orders;

public record SearchOrderRequest : SearchQuery
{
    public string? LocationId { get; set; }

    public OrderStatus? Status { get; set; }
}
