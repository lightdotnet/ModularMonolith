namespace StarterKit.Transfers.Contracts.StockTransfers;

public record CancelStockTransferRequest
{
    public string Reason { get; set; } = null!;
}

public sealed class CancelStockTransferRequestValidator : AbstractValidator<CancelStockTransferRequest>
{
    public CancelStockTransferRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
