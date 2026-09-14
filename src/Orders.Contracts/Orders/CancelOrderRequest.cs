namespace StarterKit.Orders.Contracts.Orders;

public record CancelOrderRequest
{
    public string Reason { get; set; } = null!;
}

public sealed class CancelOrderRequestValidator : AbstractValidator<CancelOrderRequest>
{
    public CancelOrderRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
