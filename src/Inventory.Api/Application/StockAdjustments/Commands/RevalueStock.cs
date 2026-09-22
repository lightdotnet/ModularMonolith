using StarterKit.Inventory.Api.Domain.StockAdjustments;
using StarterKit.Inventory.Api.Services;
using StarterKit.Locations.Contracts.Services;

namespace StarterKit.Inventory.Api.Application.StockAdjustments.Commands;

internal sealed record RevalueStockCommand(
    RevalueStockRequest Model,
    string PerformedByUserId) : ICommand<IResult>;

internal sealed class RevalueStockCommandValidator : AbstractValidator<RevalueStockCommand>
{
    public RevalueStockCommandValidator()
    {
        RuleFor(x => x.PerformedByUserId).NotEmpty();
        RuleFor(x => x.Model).SetValidator(new RevalueStockRequestValidator());
    }
}

internal class RevalueStockCommandHandler(
    StockLedger ledger,
    ILocationDirectoryService locationDirectoryService)
    : ICommandHandler<RevalueStockCommand, IResult>
{
    public async Task<IResult> Handle(
        RevalueStockCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        if (!await locationDirectoryService.ExistsAsync(model.LocationId, cancellationToken))
            return Result.NotFound($"Location {model.LocationId} not found");

        // The ledger prices the revaluation against the level (new total = on hand x new unit cost) and
        // rejects it when nothing is on hand.
        await ledger.ApplyAsync(
            [
                new StockMovement(
                    model.ProductId,
                    model.LocationId,
                    0,
                    StockAdjustmentReason.CostRevaluation,
                    request.PerformedByUserId,
                    Note: model.Note,
                    UnitCostBase: model.UnitCost),
            ],
            cancellationToken);

        return Result.Success();
    }
}
