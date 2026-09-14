using StarterKit.Catalog.Contracts.Common;
using StarterKit.Catalog.Contracts.Services;
using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Shared.ValueObjects;

namespace StarterKit.Orders.Api.Application.Orders.Commands;

internal sealed record AddOrderLineCommand(
    long OrderId,
    AddOrderLineRequest Model) : ICommand<IResult>;

internal sealed class AddOrderLineCommandValidator : AbstractValidator<AddOrderLineCommand>
{
    public AddOrderLineCommandValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new AddOrderLineRequestValidator());
    }
}

internal class AddOrderLineCommandHandler(
    OrdersDbContext context,
    ICatalogPricingService catalogPricingService)
    : ICommandHandler<AddOrderLineCommand, IResult>
{
    public async Task<IResult> Handle(
        AddOrderLineCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Orders
            .Include(x => x.Lines)
            .Where(new OrderByIdSpec(request.OrderId))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Order {request.OrderId} not found");

        var model = request.Model;

        var priceInfo = await catalogPricingService.GetPriceInfoAsync(model.ProductId, cancellationToken);

        if (priceInfo is null || priceInfo.Status != ProductStatus.Active)
            return Result.NotFound($"Product {model.ProductId} not found or is not active");

        var unitPrice = new Money(priceInfo.Price, priceInfo.Currency);
        var vatRate = new VatPercentage(priceInfo.VatRate);

        var requestedSalePrice = model.RequestedSalePrice.HasValue
            ? new Money(model.RequestedSalePrice.Value, priceInfo.Currency)
            : null;

        entity.AddLine(
            model.ProductId,
            priceInfo.ProductName,
            // Product.Sku is nullable since it can be cleared (RemoveProductSkuCommand); an order
            // line still needs a plain string snapshot, so an already-cleared SKU snapshots as
            // empty rather than null — not spelled out by the retype plan, smallest reasonable call.
            priceInfo.Sku ?? string.Empty,
            model.Quantity,
            unitPrice,
            vatRate,
            requestedSalePrice);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
