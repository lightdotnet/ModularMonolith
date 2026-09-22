namespace StarterKit.Inventory.Contracts.Stock;

public record RecordStockMovementRequest
{
    public long ProductId { get; set; }

    public string LocationId { get; set; } = null!;

    /// <summary>Positive adds stock, negative removes it; zero is rejected.</summary>
    public int QuantityDelta { get; set; }

    /// <summary>
    /// Base-currency unit cost of an inbound movement. When omitted it defaults to the current average
    /// cost; it is required when nothing is on hand. Needs only the manage permission. Ignored for outbound
    /// movements (always at average).
    /// </summary>
    public decimal? UnitCost { get; set; }

    public string? Note { get; set; }
}

public sealed class RecordStockMovementRequestValidator : AbstractValidator<RecordStockMovementRequest>
{
    public RecordStockMovementRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.LocationId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.QuantityDelta)
            .NotEqual(0)
            .InclusiveBetween(-1_000_000_000, 1_000_000_000);
        RuleFor(x => x.UnitCost)
            .InclusiveBetween(0m, 1_000_000_000m)
            .PrecisionScale(19, 4, false)
            .When(x => x.UnitCost.HasValue);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
