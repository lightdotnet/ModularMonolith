using StarterKit.Purchasing.Contracts.Common;

namespace StarterKit.Purchasing.Contracts.PurchaseReturns;

public record CreatePurchaseReturnRequest
{
    public long GoodsReceiptId { get; set; }

    public PurchaseReturnReason Reason { get; set; }

    public string? Note { get; set; }

    public IList<PurchaseReturnLineRequest> Lines { get; set; } = [];
}

public record PurchaseReturnLineRequest
{
    public long GoodsReceiptLineId { get; set; }

    public int Quantity { get; set; }

    public PurchaseReturnReason? Reason { get; set; }
}

public sealed class CreatePurchaseReturnRequestValidator : AbstractValidator<CreatePurchaseReturnRequest>
{
    public CreatePurchaseReturnRequestValidator()
    {
        RuleFor(x => x.GoodsReceiptId).GreaterThan(0);
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

public sealed class PurchaseReturnLineRequestValidator : AbstractValidator<PurchaseReturnLineRequest>
{
    public PurchaseReturnLineRequestValidator()
    {
        RuleFor(x => x.GoodsReceiptLineId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(PurchasingLimits.MaxQuantity);
        RuleFor(x => x.Reason).IsInEnum();
    }
}
