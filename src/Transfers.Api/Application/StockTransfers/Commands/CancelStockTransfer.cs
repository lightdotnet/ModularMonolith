using StarterKit.Shared;
using StarterKit.Transfers.Api.Data;
using StarterKit.Transfers.Api.Domain.StockTransfers;

namespace StarterKit.Transfers.Api.Application.StockTransfers.Commands;

internal sealed record CancelStockTransferCommand(
    long Id,
    CancelStockTransferRequest Model) : ICommand<IResult>;

internal sealed class CancelStockTransferCommandValidator : AbstractValidator<CancelStockTransferCommand>
{
    public CancelStockTransferCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new CancelStockTransferRequestValidator());
    }
}

internal class CancelStockTransferCommandHandler(
    TransfersDbContext context,
    IDateTime clock)
    : ICommandHandler<CancelStockTransferCommand, IResult>
{
    public async Task<IResult> Handle(
        CancelStockTransferCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.StockTransfers
            .Where(new StockTransferByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Transfer {request.Id} not found");

        entity.Cancel(request.Model.Reason, clock.UtcNow);

        await TransferSaving.SaveAsync(context, cancellationToken);

        return Result.Success();
    }
}
