using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using StarterKit.Shared;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders.Commands;

internal sealed record ClosePurchaseOrderCommand(
    long Id,
    ClosePurchaseOrderRequest Model,
    string CurrentUserId) : ICommand<IResult>;

internal sealed class ClosePurchaseOrderCommandValidator : AbstractValidator<ClosePurchaseOrderCommand>
{
    public ClosePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new ClosePurchaseOrderRequestValidator());
    }
}

/// <summary>Gives up the undelivered remainder of a partially received order; nothing is posted to Inventory.</summary>
internal class ClosePurchaseOrderCommandHandler(
    PurchasingDbContext context,
    IDateTime clock)
    : ICommandHandler<ClosePurchaseOrderCommand, IResult>
{
    public async Task<IResult> Handle(
        ClosePurchaseOrderCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.PurchaseOrders
            .Include(x => x.Lines)
            .Where(new PurchaseOrderByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Purchase order {request.Id} not found");

        entity.Close(
            request.Model.Reason,
            request.CurrentUserId,
            clock.UtcNow,
            await context.HasReceiptInFlightAsync(entity.Id, cancellationToken));

        await PurchasingSaving.SaveAsync(context, cancellationToken);

        return Result.Success();
    }
}
