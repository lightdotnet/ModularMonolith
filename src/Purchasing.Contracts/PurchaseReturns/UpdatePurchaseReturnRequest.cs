using StarterKit.Purchasing.Contracts.Common;

namespace StarterKit.Purchasing.Contracts.PurchaseReturns;

/// <summary>Replaces the draft return reason, note and lines.</summary>
public record UpdatePurchaseReturnRequest
{
    public PurchaseReturnReason Reason { get; set; }

    public string? Note { get; set; }

    public IList<PurchaseReturnLineRequest> Lines { get; set; } = [];
}

public sealed class UpdatePurchaseReturnRequestValidator : AbstractValidator<UpdatePurchaseReturnRequest>
{
    public UpdatePurchaseReturnRequestValidator()
    {
        RuleFor(x => x.Reason).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(1000);

        RuleFor(x => x.Lines)
            .NotEmpty()
            .Must(lines => lines.Count <= PurchasingLimits.MaxLines)
            .WithMessage($"At most {PurchasingLimits.MaxLines} lines are allowed.")
            .Must(lines => lines.Select(x => x.GoodsReceiptLineId).Distinct().Count() == lines.Count)
            .WithMessage("A receipt line can appear only once per return.");

        RuleForEach(x => x.Lines).SetValidator(new PurchaseReturnLineRequestValidator());
    }
}
