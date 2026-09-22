namespace StarterKit.Orders.Contracts.Payments;

public record RecordPaymentRequest
{
    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public string PaymentTypeId { get; set; } = null!;

    public DateTimeOffset PaidAt { get; set; }

    public string? Reference { get; set; }
}

public sealed class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        // Shape only: the payment must equal the order's own currency, which the aggregate enforces.
        RuleFor(x => x.Currency)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency must be a three-letter ISO 4217 code.");
        RuleFor(x => x.PaymentTypeId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.Reference).MaximumLength(200);
    }
}
