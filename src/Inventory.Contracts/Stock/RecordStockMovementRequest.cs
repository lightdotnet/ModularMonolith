namespace StarterKit.Inventory.Contracts.Stock;

public record RecordStockMovementRequest
{
    public long ProductId { get; set; }

    public string LocationId { get; set; } = null!;

    /// <summary>Positive adds stock, negative removes it; zero is rejected.</summary>
    public int QuantityDelta { get; set; }

    public string? Note { get; set; }
}

public sealed class RecordStockMovementRequestValidator : AbstractValidator<RecordStockMovementRequest>
{
    public RecordStockMovementRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.LocationId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.QuantityDelta).NotEqual(0);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
