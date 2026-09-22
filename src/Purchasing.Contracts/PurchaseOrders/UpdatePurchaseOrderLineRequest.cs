using StarterKit.Purchasing.Contracts.Common;

namespace StarterKit.Purchasing.Contracts.PurchaseOrders;

public record UpdatePurchaseOrderLineRequest
{
    public int Quantity { get; set; }

    /// <summary>Unit cost in the base currency.</summary>
    public decimal UnitCost { get; set; }
}

public sealed class UpdatePurchaseOrderLineRequestValidator : AbstractValidator<UpdatePurchaseOrderLineRequest>
{
    public UpdatePurchaseOrderLineRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(PurchasingLimits.MaxQuantity);
        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(PurchasingLimits.MaxAmount)
            .PrecisionScale(19, 4, false);
    }
}
