using StarterKit.Transfers.Api.Data;
using StarterKit.Transfers.Api.Domain.StockTransfers;

namespace StarterKit.Transfers.Api.Application.StockTransfers.Commands;

internal sealed record UpdateStockTransferLineCommand(
    long TransferId,
    long TransferLineId,
    UpdateStockTransferLineRequest Model) : ICommand<IResult>;

internal sealed class UpdateStockTransferLineCommandValidator : AbstractValidator<UpdateStockTransferLineCommand>
{
    public UpdateStockTransferLineCommandValidator()
    {
        RuleFor(x => x.TransferId).GreaterThan(0);
        RuleFor(x => x.TransferLineId).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new UpdateStockTransferLineRequestValidator());
    }
}

internal class UpdateStockTransferLineCommandHandler(TransfersDbContext context)
    : ICommandHandler<UpdateStockTransferLineCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateStockTransferLineCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.StockTransfers
            .Include(x => x.Lines)
            .Where(new StockTransferByIdSpec(request.TransferId))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Transfer {request.TransferId} not found");

        entity.UpdateLine(request.TransferLineId, request.Model.Quantity);

        await TransferSaving.SaveAsync(context, cancellationToken);

        return Result.Success();
    }
}
