using StarterKit.Shared.Constants;

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
        RuleFor(x => x.Currency).NotEmpty().Equal(CurrencyConstants.Default);
        RuleFor(x => x.PaymentTypeId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.Reference).MaximumLength(200);
    }
}
