using StarterKit.Transfers.Api.Data;
using StarterKit.Transfers.Api.Domain.StockTransfers;
using StarterKit.Transfers.Api.Services;

namespace StarterKit.Transfers.Api.Application.StockTransfers.Commands;

internal sealed record UpdateStockTransferCommand(
    long Id,
    UpdateStockTransferRequest Model) : ICommand<IResult>;

internal sealed class UpdateStockTransferCommandValidator : AbstractValidator<UpdateStockTransferCommand>
{
    public UpdateStockTransferCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new UpdateStockTransferRequestValidator());
    }
}

internal class UpdateStockTransferCommandHandler(
    TransfersDbContext context,
    TransferLocationResolver locationResolver)
    : ICommandHandler<UpdateStockTransferCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateStockTransferCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.StockTransfers
            .Where(new StockTransferByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Transfer {request.Id} not found");

        var model = request.Model;

        var locations = await locationResolver.ResolveAsync(
            model.SourceLocationId,
            model.DestinationLocationId,
            cancellationToken);

        if (locations.Failed)
            return locations.ToFailure();

        entity.UpdateHeader(
            locations.Source!.Id,
            locations.Source.Name,
            locations.Destination!.Id,
            locations.Destination.Name,
            model.Note);

        await TransferSaving.SaveAsync(context, cancellationToken);

        return Result.Success();
    }
}
