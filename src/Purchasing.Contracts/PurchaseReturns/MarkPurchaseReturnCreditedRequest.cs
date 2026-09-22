using StarterKit.Purchasing.Contracts.Common;

namespace StarterKit.Purchasing.Contracts.PurchaseReturns;

/// <summary>Records the credit note issued by the supplier against a posted return; informational bookkeeping.</summary>
public record MarkPurchaseReturnCreditedRequest
{
    public string CreditNoteNumber { get; set; } = null!;

    /// <summary>Credited amount in the base currency.</summary>
    public decimal CreditAmount { get; set; }
}

public sealed class MarkPurchaseReturnCreditedRequestValidator : AbstractValidator<MarkPurchaseReturnCreditedRequest>
{
    public MarkPurchaseReturnCreditedRequestValidator()
    {
        RuleFor(x => x.CreditNoteNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CreditAmount)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(PurchasingLimits.MaxAmount)
            .PrecisionScale(19, 4, false);
    }
}
