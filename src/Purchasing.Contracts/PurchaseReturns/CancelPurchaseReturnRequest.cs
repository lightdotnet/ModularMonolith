namespace StarterKit.Purchasing.Contracts.PurchaseReturns;

public record CancelPurchaseReturnRequest
{
    public string Reason { get; set; } = null!;
}

public sealed class CancelPurchaseReturnRequestValidator : AbstractValidator<CancelPurchaseReturnRequest>
{
    public CancelPurchaseReturnRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
