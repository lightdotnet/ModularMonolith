using StarterKit.Catalog.Contracts.Services;
using StarterKit.Transfers.Api.Data;
using StarterKit.Transfers.Api.Domain.StockTransfers;

namespace StarterKit.Transfers.Api.Application.StockTransfers.Commands;

internal sealed record AddStockTransferLineCommand(
    long TransferId,
    AddStockTransferLineRequest Model) : ICommand<IResult>;

internal sealed class AddStockTransferLineCommandValidator : AbstractValidator<AddStockTransferLineCommand>
{
    public AddStockTransferLineCommandValidator()
    {
        RuleFor(x => x.TransferId).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new AddStockTransferLineRequestValidator());
    }
}

internal class AddStockTransferLineCommandHandler(
    TransfersDbContext context,
    ICatalogPricingService catalogPricingService)
    : ICommandHandler<AddStockTransferLineCommand, IResult>
{
    public async Task<IResult> Handle(
        AddStockTransferLineCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.StockTransfers
            .Include(x => x.Lines)
            .Where(new StockTransferByIdSpec(request.TransferId))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Transfer {request.TransferId} not found");

        var model = request.Model;

        // Existence only: pricing is irrelevant to a stock move, and a product that has since been
        // deactivated can still be moved between locations.
        var product = await catalogPricingService.GetPriceInfoAsync(model.ProductId, cancellationToken);

        if (product is null)
            return Result.NotFound($"Product {model.ProductId} not found");

        entity.AddLine(
            model.ProductId,
            product.ProductName,
            // Product.Sku is nullable (it can be cleared); the line snapshot is a plain string.
            product.Sku ?? string.Empty,
            model.Quantity);

        await TransferSaving.SaveAsync(context, cancellationToken);

        return Result.Success();
    }
}
