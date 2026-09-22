namespace StarterKit.Transfers.Contracts.StockTransfers;

public record UpdateStockTransferLineRequest
{
    public int Quantity { get; set; }
}

public sealed class UpdateStockTransferLineRequestValidator : AbstractValidator<UpdateStockTransferLineRequest>
{
    public UpdateStockTransferLineRequestValidator()
    {
        RuleFor(x => x.Quantity).InclusiveBetween(1, StockTransferLimits.MaxLineQuantity);
    }
}
