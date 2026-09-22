namespace StarterKit.Transfers.Contracts.StockTransfers;

public record UpdateStockTransferRequest
{
    public string SourceLocationId { get; set; } = null!;

    public string DestinationLocationId { get; set; } = null!;

    public string? Note { get; set; }
}

public sealed class UpdateStockTransferRequestValidator : AbstractValidator<UpdateStockTransferRequest>
{
    public UpdateStockTransferRequestValidator()
    {
        RuleFor(x => x.SourceLocationId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.DestinationLocationId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
