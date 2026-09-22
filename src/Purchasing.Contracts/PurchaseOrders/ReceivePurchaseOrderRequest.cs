using StarterKit.Purchasing.Contracts.Common;

namespace StarterKit.Purchasing.Contracts.PurchaseOrders;

/// <summary>
/// <see cref="DeliveryNoteRef"/> is the delivery note number printed by the supplier: when supplied it
/// is unique per purchase order, so a resubmitted delivery maps back onto the goods receipt already
/// recorded for it instead of receiving twice.
/// </summary>
public record ReceivePurchaseOrderRequest
{
    public string? DeliveryNoteRef { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public IList<ReceivePurchaseOrderLineRequest> Lines { get; set; } = [];
}

public record ReceivePurchaseOrderLineRequest
{
    public long PurchaseOrderLineId { get; set; }

    public int Quantity { get; set; }
}

public sealed class ReceivePurchaseOrderRequestValidator : AbstractValidator<ReceivePurchaseOrderRequest>
{
    public ReceivePurchaseOrderRequestValidator()
    {
        RuleFor(x => x.DeliveryNoteRef).MaximumLength(100);

        RuleFor(x => x.ReceivedAt).NotEmpty();

        RuleFor(x => x.Lines)
            .NotEmpty()
            .Must(lines => lines.Count <= PurchasingLimits.MaxLines)
            .WithMessage($"At most {PurchasingLimits.MaxLines} lines are allowed.")
            .Must(lines => lines.Select(x => x.PurchaseOrderLineId).Distinct().Count() == lines.Count)
            .WithMessage("A purchase order line can appear only once per receipt.");

        RuleForEach(x => x.Lines).SetValidator(new ReceivePurchaseOrderLineRequestValidator());
    }
}

public sealed class ReceivePurchaseOrderLineRequestValidator : AbstractValidator<ReceivePurchaseOrderLineRequest>
{
    public ReceivePurchaseOrderLineRequestValidator()
    {
        RuleFor(x => x.PurchaseOrderLineId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(PurchasingLimits.MaxQuantity);
    }
}
