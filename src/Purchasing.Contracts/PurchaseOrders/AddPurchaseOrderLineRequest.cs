using StarterKit.Purchasing.Contracts.Common;

namespace StarterKit.Purchasing.Contracts.PurchaseOrders;

public record AddPurchaseOrderLineRequest
{
    public long ProductId { get; set; }

    public int Quantity { get; set; }

    /// <summary>Unit cost in the base currency.</summary>
    public decimal UnitCost { get; set; }
}

public sealed class AddPurchaseOrderLineRequestValidator : AbstractValidator<AddPurchaseOrderLineRequest>
{
    public AddPurchaseOrderLineRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(PurchasingLimits.MaxQuantity);
        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(PurchasingLimits.MaxAmount)
            .PrecisionScale(19, 4, false);
    }
}
