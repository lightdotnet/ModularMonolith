using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders.Commands;

internal sealed record RemovePurchaseOrderLineCommand(
    long PurchaseOrderId,
    long PurchaseOrderLineId,
    string CurrentUserId,
    bool CanManage) : ICommand<IResult>;

internal sealed class RemovePurchaseOrderLineCommandValidator : AbstractValidator<RemovePurchaseOrderLineCommand>
{
    public RemovePurchaseOrderLineCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).GreaterThan(0);
        RuleFor(x => x.PurchaseOrderLineId).GreaterThan(0);
    }
}

internal class RemovePurchaseOrderLineCommandHandler(PurchasingDbContext context)
    : ICommandHandler<RemovePurchaseOrderLineCommand, IResult>
{
    public async Task<IResult> Handle(
        RemovePurchaseOrderLineCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.PurchaseOrders
            .Include(x => x.Lines)
            .Where(new PurchaseOrderByIdSpec(request.PurchaseOrderId))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Purchase order {request.PurchaseOrderId} not found");

        entity.EnsureOwnerOrManager(request.CurrentUserId, request.CanManage);

        entity.RemoveLine(request.PurchaseOrderLineId);

        await PurchasingSaving.SaveAsync(context, cancellationToken);

        return Result.Success();
    }
}
