namespace StarterKit.Orders.Contracts.Payments;

public record VoidPaymentRequest
{
    public string Reason { get; set; } = null!;
}

public sealed class VoidPaymentRequestValidator : AbstractValidator<VoidPaymentRequest>
{
    public VoidPaymentRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
