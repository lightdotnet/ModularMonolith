namespace StarterKit.Purchasing.Contracts.PurchaseOrders;

public record UpdatePurchaseOrderRequest
{
    public long SupplierId { get; set; }

    public string LocationId { get; set; } = null!;

    public DateTimeOffset? ExpectedAt { get; set; }

    public string? Note { get; set; }
}

public sealed class UpdatePurchaseOrderRequestValidator : AbstractValidator<UpdatePurchaseOrderRequest>
{
    public UpdatePurchaseOrderRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.LocationId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
