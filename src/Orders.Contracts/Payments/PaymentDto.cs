using StarterKit.Orders.Contracts.Common;

namespace StarterKit.Orders.Contracts.Payments;

public class PaymentDto : BaseDto<long>
{
    public long OrderId { get; set; }

    public string OrderCode { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public PaymentMethod Method { get; set; }

    public DateTimeOffset PaidAt { get; set; }

    public string? Reference { get; set; }

    public string RecordedByUserId { get; set; } = null!;

    public bool IsVoided { get; set; }

    public DateTimeOffset? VoidedAt { get; set; }

    public string? VoidReason { get; set; }
}
