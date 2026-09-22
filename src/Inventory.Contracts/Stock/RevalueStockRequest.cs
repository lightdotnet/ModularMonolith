namespace StarterKit.Inventory.Contracts.Stock;

public record RevalueStockRequest
{
    public long ProductId { get; set; }

    public string LocationId { get; set; } = null!;

    /// <summary>New base-currency average unit cost for the whole on-hand quantity; must be positive.</summary>
    public decimal UnitCost { get; set; }

    public string? Note { get; set; }
}

public sealed class RevalueStockRequestValidator : AbstractValidator<RevalueStockRequest>
{
    public RevalueStockRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.LocationId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.UnitCost)
            .GreaterThan(0m)
            .LessThanOrEqualTo(1_000_000_000m)
            .PrecisionScale(19, 4, false);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
