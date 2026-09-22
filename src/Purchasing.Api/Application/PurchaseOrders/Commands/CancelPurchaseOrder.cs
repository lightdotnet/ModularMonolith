using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using StarterKit.Shared;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders.Commands;

internal sealed record CancelPurchaseOrderCommand(
    long Id,
    CancelPurchaseOrderRequest Model,
    string CurrentUserId,
    bool CanManage) : ICommand<IResult>;

internal sealed class CancelPurchaseOrderCommandValidator : AbstractValidator<CancelPurchaseOrderCommand>
{
    public CancelPurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new CancelPurchaseOrderRequestValidator());
    }
}

/// <summary>
/// Cancels a draft, rejected, or approved-without-receipts order. An approved order is cancelled locally
/// only: its approval workflow is already finished. A pending order has to be withdrawn first.
/// </summary>
internal class CancelPurchaseOrderCommandHandler(
    PurchasingDbContext context,
    IDateTime clock)
    : ICommandHandler<CancelPurchaseOrderCommand, IResult>
{
    public async Task<IResult> Handle(
        CancelPurchaseOrderCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.PurchaseOrders
            .Include(x => x.Lines)
            .Where(new PurchaseOrderByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Purchase order {request.Id} not found");

        entity.Cancel(
            request.Model.Reason,
            request.CurrentUserId,
            request.CanManage,
            clock.UtcNow,
            await context.HasReceiptInFlightAsync(entity.Id, cancellationToken));

        await PurchasingSaving.SaveAsync(context, cancellationToken);

        return Result.Success();
    }
}
