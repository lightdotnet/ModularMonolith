namespace StarterKit.Transfers.Contracts.StockTransfers;

/// <summary>
/// <see cref="ClientRequestId"/> is a caller-generated key (for example a GUID) that makes a
/// resubmitted receipt a no-op instead of a double receipt.
/// </summary>
public record ReceiveStockTransferRequest
{
    public string ClientRequestId { get; set; } = null!;

    public IList<ReceiveStockTransferLineRequest> Lines { get; set; } = [];
}

public record ReceiveStockTransferLineRequest
{
    public long TransferLineId { get; set; }

    public int Quantity { get; set; }
}

public sealed class ReceiveStockTransferRequestValidator : AbstractValidator<ReceiveStockTransferRequest>
{
    public ReceiveStockTransferRequestValidator()
    {
        RuleFor(x => x.ClientRequestId).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Lines)
            .NotEmpty()
            .Must(lines => lines.Count <= StockTransferLimits.MaxLinesPerRequest)
            .WithMessage($"A receipt can have at most {StockTransferLimits.MaxLinesPerRequest} lines.")
            .Must(lines => lines.Select(x => x.TransferLineId).Distinct().Count() == lines.Count)
            .WithMessage("A transfer line can appear only once per receipt.");

        RuleForEach(x => x.Lines).SetValidator(new ReceiveStockTransferLineRequestValidator());
    }
}

public sealed class ReceiveStockTransferLineRequestValidator : AbstractValidator<ReceiveStockTransferLineRequest>
{
    public ReceiveStockTransferLineRequestValidator()
    {
        RuleFor(x => x.TransferLineId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, StockTransferLimits.MaxLineQuantity);
    }
}
