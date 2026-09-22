using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using StarterKit.Purchasing.Api.Services;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders.Commands;

internal sealed record UpdatePurchaseOrderCommand(
    long Id,
    UpdatePurchaseOrderRequest Model,
    string CurrentUserId,
    bool CanManage) : ICommand<IResult>;

internal sealed class UpdatePurchaseOrderCommandValidator : AbstractValidator<UpdatePurchaseOrderCommand>
{
    public UpdatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new UpdatePurchaseOrderRequestValidator());
    }
}

/// <summary>
/// Header edits are allowed on a draft or rejected order. A supplier or location is only re-validated
/// (existence, still active) when it actually changes, so an order stays editable after its supplier or
/// location was later deactivated.
/// </summary>
internal class UpdatePurchaseOrderCommandHandler(
    PurchasingDbContext context,
    ReceivingLocationResolver locationResolver)
    : ICommandHandler<UpdatePurchaseOrderCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdatePurchaseOrderCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.PurchaseOrders
            .Where(new PurchaseOrderByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Purchase order {request.Id} not found");

        entity.EnsureOwnerOrManager(request.CurrentUserId, request.CanManage);

        var model = request.Model;

        // Sanity only: an expected date long before the order existed is almost certainly a typo.
        if (model.ExpectedAt.HasValue && model.ExpectedAt.Value < entity.Created.AddDays(-1))
            return Result.Error("The expected date cannot be before the purchase order was created.");

        var supplierId = entity.SupplierId;
        var supplierName = entity.SupplierName;

        if (model.SupplierId != entity.SupplierId)
        {
            var supplier = await context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == model.SupplierId, cancellationToken);

            if (supplier is null)
                return Result.NotFound($"Supplier {model.SupplierId} not found");

            if (!supplier.IsActive)
                return Result.Error($"Supplier {supplier.Name} is not active");

            supplierId = supplier.Id;
            supplierName = supplier.Name;
        }

        var locationId = entity.LocationId;
        var locationName = entity.LocationName;

        if (!string.Equals(model.LocationId.Trim(), entity.LocationId, StringComparison.Ordinal))
        {
            var location = await locationResolver.ResolveAsync(model.LocationId, cancellationToken);

            if (location.Failed)
                return location.ToFailure();

            locationId = location.Location!.Id;
            locationName = location.Location.Name;
        }

        entity.UpdateHeader(
            supplierId,
            supplierName,
            locationId,
            locationName,
            model.ExpectedAt,
            model.Note);

        await PurchasingSaving.SaveAsync(context, cancellationToken);

        return Result.Success();
    }
}
