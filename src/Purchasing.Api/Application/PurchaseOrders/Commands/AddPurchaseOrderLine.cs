using StarterKit.Catalog.Contracts.Services;
using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders.Commands;

internal sealed record AddPurchaseOrderLineCommand(
    long PurchaseOrderId,
    AddPurchaseOrderLineRequest Model,
    string CurrentUserId,
    bool CanManage) : ICommand<IResult>;

internal sealed class AddPurchaseOrderLineCommandValidator : AbstractValidator<AddPurchaseOrderLineCommand>
{
    public AddPurchaseOrderLineCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new AddPurchaseOrderLineRequestValidator());
    }
}

internal class AddPurchaseOrderLineCommandHandler(
    PurchasingDbContext context,
    ICatalogPricingService catalogPricingService)
    : ICommandHandler<AddPurchaseOrderLineCommand, IResult>
{
    public async Task<IResult> Handle(
        AddPurchaseOrderLineCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.PurchaseOrders
            .Include(x => x.Lines)
            .Where(new PurchaseOrderByIdSpec(request.PurchaseOrderId))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Purchase order {request.PurchaseOrderId} not found");

        entity.EnsureOwnerOrManager(request.CurrentUserId, request.CanManage);

        var model = request.Model;

        // Existence only: the catalog sell price is irrelevant to a purchase cost, which the buyer
        // supplies on the line.
        var product = await catalogPricingService.GetPriceInfoAsync(model.ProductId, cancellationToken);

        if (product is null)
            return Result.NotFound($"Product {model.ProductId} not found");

        entity.AddLine(
            model.ProductId,
            product.ProductName,
            // Product.Sku is nullable (it can be cleared); the line snapshot is a plain string.
            product.Sku ?? string.Empty,
            model.Quantity,
            new Money(model.UnitCost, CurrencyConstants.Default));

        await PurchasingSaving.SaveAsync(context, cancellationToken);

        return Result.Success();
    }
}
