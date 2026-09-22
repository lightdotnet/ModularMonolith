namespace StarterKit.Purchasing.Contracts.PurchaseOrders;

public record CancelPurchaseOrderRequest
{
    public string Reason { get; set; } = null!;
}

public sealed class CancelPurchaseOrderRequestValidator : AbstractValidator<CancelPurchaseOrderRequest>
{
    public CancelPurchaseOrderRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
