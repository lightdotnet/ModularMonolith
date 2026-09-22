using StarterKit.Inventory.Api.Domain.StockAdjustments;
using StarterKit.Inventory.Api.Services;
using StarterKit.Locations.Contracts.Services;

namespace StarterKit.Inventory.Api.Application.StockAdjustments.Commands;

internal sealed record RecordStockMovementCommand(
    RecordStockMovementRequest Model,
    string PerformedByUserId) : ICommand<IResult>;

internal sealed class RecordStockMovementCommandValidator : AbstractValidator<RecordStockMovementCommand>
{
    public RecordStockMovementCommandValidator()
    {
        RuleFor(x => x.PerformedByUserId).NotEmpty();
        RuleFor(x => x.Model).SetValidator(new RecordStockMovementRequestValidator());
    }
}

internal class RecordStockMovementCommandHandler(
    StockLedger ledger,
    ILocationDirectoryService locationDirectoryService)
    : ICommandHandler<RecordStockMovementCommand, IResult>
{
    public async Task<IResult> Handle(
        RecordStockMovementCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        if (!await locationDirectoryService.ExistsAsync(model.LocationId, cancellationToken))
            return Result.NotFound($"Location {model.LocationId} not found");

        await ledger.ApplyAsync(
            [
                new StockMovement(
                    model.ProductId,
                    model.LocationId,
                    model.QuantityDelta,
                    StockAdjustmentReason.ManualAdjustment,
                    request.PerformedByUserId,
                    Note: model.Note,
                    // Only an inbound movement carries a cost; an omitted one defaults to the current
                    // average inside the ledger, which rejects it when nothing is on hand.
                    UnitCostBase: model.QuantityDelta > 0 ? model.UnitCost : null),
            ],
            cancellationToken);

        return Result.Success();
    }
}
