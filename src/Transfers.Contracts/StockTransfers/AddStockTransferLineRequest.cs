namespace StarterKit.Transfers.Contracts.StockTransfers;

public record AddStockTransferLineRequest
{
    public long ProductId { get; set; }

    public int Quantity { get; set; }
}

public sealed class AddStockTransferLineRequestValidator : AbstractValidator<AddStockTransferLineRequest>
{
    public AddStockTransferLineRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, StockTransferLimits.MaxLineQuantity);
    }
}
