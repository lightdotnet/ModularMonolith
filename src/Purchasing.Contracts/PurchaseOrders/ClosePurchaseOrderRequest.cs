namespace StarterKit.Purchasing.Contracts.PurchaseOrders;

public record ClosePurchaseOrderRequest
{
    public string Reason { get; set; } = null!;
}

public sealed class ClosePurchaseOrderRequestValidator : AbstractValidator<ClosePurchaseOrderRequest>
{
    public ClosePurchaseOrderRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
