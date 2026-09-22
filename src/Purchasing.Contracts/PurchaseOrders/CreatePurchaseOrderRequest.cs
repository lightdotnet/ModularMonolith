namespace StarterKit.Purchasing.Contracts.PurchaseOrders;

public record CreatePurchaseOrderRequest
{
    public long SupplierId { get; set; }

    /// <summary>The receiving location.</summary>
    public string LocationId { get; set; } = null!;

    public DateTimeOffset? ExpectedAt { get; set; }

    public string? Note { get; set; }
}

public sealed class CreatePurchaseOrderRequestValidator : AbstractValidator<CreatePurchaseOrderRequest>
{
    public CreatePurchaseOrderRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.LocationId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
