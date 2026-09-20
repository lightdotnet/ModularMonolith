using Microsoft.Extensions.Logging;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Shared;

namespace StarterKit.Orders.Api.Application.Orders.Commands;

internal sealed record CancelOrderCommand(
    long Id,
    CancelOrderRequest Model,
    string CancelledByUserId) : ICommand<IResult>;

internal sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.CancelledByUserId).NotEmpty();
        RuleFor(x => x.Model).SetValidator(new CancelOrderRequestValidator());
    }
}

/// <summary>
/// The cancel is committed first; stock is then restored best-effort. The restore is idempotent, so
/// a failure here is only logged — it under-counts stock until a retry, and never blocks the cancel.
/// </summary>
internal class CancelOrderCommandHandler(
    OrdersDbContext context,
    IInventoryService inventoryService,
    IDateTime clock,
    ILogger<CancelOrderCommandHandler> logger)
    : ICommandHandler<CancelOrderCommand, IResult>
{
    public async Task<IResult> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Orders
            .Where(new OrderByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Order {request.Id} not found");

        var wasPlaced = entity.PlacedAt is not null;

        entity.Cancel(request.CancelledByUserId, request.Model.Reason, clock.UtcNow);

        await context.SaveChangesAsync(cancellationToken);

        if (wasPlaced)
        {
            try
            {
                await inventoryService.RestoreForOrderAsync(
                    entity.Id,
                    request.CancelledByUserId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to restore stock for cancelled order {OrderId}.",
                    entity.Id);
            }
        }

        return Result.Success();
    }
}
