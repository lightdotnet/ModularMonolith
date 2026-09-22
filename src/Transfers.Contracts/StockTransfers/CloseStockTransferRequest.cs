namespace StarterKit.Transfers.Contracts.StockTransfers;

public record CloseStockTransferRequest
{
    public string Reason { get; set; } = null!;
}

public sealed class CloseStockTransferRequestValidator : AbstractValidator<CloseStockTransferRequest>
{
    public CloseStockTransferRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
