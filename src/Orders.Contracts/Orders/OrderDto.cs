using StarterKit.Orders.Contracts.Common;

namespace StarterKit.Orders.Contracts.Orders;

/// <summary>
/// <see cref="Subtotal"/>/<see cref="DiscountAmount"/>/<see cref="FeesTotal"/>/<see cref="Total"/>
/// mirror the domain's own computed roll-ups (<c>Order.Subtotal</c> etc.) — plain decimals, not a
/// nested <c>Money</c> shape, same flattening rule as <see cref="OrderLineDto"/>'s
/// <see cref="OrderLineDto.UnitPrice"/>/<see cref="OrderLineDto.VatRate"/>.
/// </summary>
public class OrderDto : BaseDto<long>
{
    public string LocationId { get; set; } = null!;

    public string? MemberId { get; set; }

    public string OrderCode { get; set; } = null!;

    public string? ExternalReferenceCode { get; set; }

    public OrderStatus Status { get; set; }

    public OrderDiscountKind? DiscountKind { get; set; }

    public decimal? DiscountValue { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FeesTotal { get; set; }

    public decimal Total { get; set; }

    public decimal AmountPaid { get; set; }

    public string Currency { get; set; } = null!;

    public DateTimeOffset? PlacedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public DateTimeOffset? FulfilledAt { get; set; }

    public string? CancelledReason { get; set; }

    public IList<OrderLineDto> Lines { get; set; } = [];

    public IList<OrderFeeDto> Fees { get; set; } = [];
}
