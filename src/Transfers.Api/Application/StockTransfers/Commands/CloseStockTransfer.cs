using StarterKit.Shared;
using StarterKit.Transfers.Api.Data;
using StarterKit.Transfers.Api.Domain.StockTransfers;

namespace StarterKit.Transfers.Api.Application.StockTransfers.Commands;

internal sealed record CloseStockTransferCommand(
    long Id,
    CloseStockTransferRequest Model) : ICommand<IResult>;

internal sealed class CloseStockTransferCommandValidator : AbstractValidator<CloseStockTransferCommand>
{
    public CloseStockTransferCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new CloseStockTransferRequestValidator());
    }
}

/// <summary>Local variance only: the remainder is written off on the transfer, nothing is posted to Inventory.</summary>
internal class CloseStockTransferCommandHandler(
    TransfersDbContext context,
    IDateTime clock)
    : ICommandHandler<CloseStockTransferCommand, IResult>
{
    public async Task<IResult> Handle(
        CloseStockTransferCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.StockTransfers
            .Include(x => x.Lines)
            .Include(x => x.Receipts)
            .Where(new StockTransferByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Transfer {request.Id} not found");

        entity.Close(request.Model.Reason, clock.UtcNow);

        await TransferSaving.SaveAsync(context, cancellationToken);

        return Result.Success();
    }
}
