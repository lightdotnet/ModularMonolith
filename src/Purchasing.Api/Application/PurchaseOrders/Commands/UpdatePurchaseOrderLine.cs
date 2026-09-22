using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders.Commands;

internal sealed record UpdatePurchaseOrderLineCommand(
    long PurchaseOrderId,
    long PurchaseOrderLineId,
    UpdatePurchaseOrderLineRequest Model,
    string CurrentUserId,
    bool CanManage) : ICommand<IResult>;

internal sealed class UpdatePurchaseOrderLineCommandValidator : AbstractValidator<UpdatePurchaseOrderLineCommand>
{
    public UpdatePurchaseOrderLineCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).GreaterThan(0);
        RuleFor(x => x.PurchaseOrderLineId).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new UpdatePurchaseOrderLineRequestValidator());
    }
}

internal class UpdatePurchaseOrderLineCommandHandler(PurchasingDbContext context)
    : ICommandHandler<UpdatePurchaseOrderLineCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdatePurchaseOrderLineCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.PurchaseOrders
            .Include(x => x.Lines)
            .Where(new PurchaseOrderByIdSpec(request.PurchaseOrderId))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Purchase order {request.PurchaseOrderId} not found");

        entity.EnsureOwnerOrManager(request.CurrentUserId, request.CanManage);

        entity.UpdateLine(
            request.PurchaseOrderLineId,
            request.Model.Quantity,
            new Money(request.Model.UnitCost, CurrencyConstants.Default));

        await PurchasingSaving.SaveAsync(context, cancellationToken);

        return Result.Success();
    }
}
