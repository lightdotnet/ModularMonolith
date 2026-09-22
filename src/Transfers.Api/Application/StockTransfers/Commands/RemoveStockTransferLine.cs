using StarterKit.Transfers.Api.Data;
using StarterKit.Transfers.Api.Domain.StockTransfers;

namespace StarterKit.Transfers.Api.Application.StockTransfers.Commands;

internal sealed record RemoveStockTransferLineCommand(
    long TransferId,
    long TransferLineId) : ICommand<IResult>;

internal sealed class RemoveStockTransferLineCommandValidator : AbstractValidator<RemoveStockTransferLineCommand>
{
    public RemoveStockTransferLineCommandValidator()
    {
        RuleFor(x => x.TransferId).GreaterThan(0);
        RuleFor(x => x.TransferLineId).GreaterThan(0);
    }
}

internal class RemoveStockTransferLineCommandHandler(TransfersDbContext context)
    : ICommandHandler<RemoveStockTransferLineCommand, IResult>
{
    public async Task<IResult> Handle(
        RemoveStockTransferLineCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.StockTransfers
            .Include(x => x.Lines)
            .Where(new StockTransferByIdSpec(request.TransferId))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Transfer {request.TransferId} not found");

        entity.RemoveLine(request.TransferLineId);

        await TransferSaving.SaveAsync(context, cancellationToken);

        return Result.Success();
    }
}
