using Microsoft.Extensions.Logging;
using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Inventory.Contracts.Stock;
using StarterKit.Orders.Contracts.Events;
using StarterKit.Shared;

namespace StarterKit.Orders.Api.Application.Orders.Commands;

internal sealed record PlaceOrderCommand(
    long Id,
    string PlacedByUserId) : ICommand<IResult>;

internal sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.PlacedByUserId).NotEmpty();
    }
}

/// <summary>
/// Stock is decremented before the order is saved: an oversell throws a conflict while nothing is
/// persisted in Orders. There is no shared transaction with Inventory, so if the Orders save then
/// fails the decrement is compensated with a restore (idempotent) before the failure is rethrown.
/// </summary>
internal class PlaceOrderCommandHandler(
    OrdersDbContext context,
    IInventoryService inventoryService,
    IDateTime clock,
    IPublisher publisher,
    ILogger<PlaceOrderCommandHandler> logger)
    : ICommandHandler<PlaceOrderCommand, IResult>
{
    public async Task<IResult> Handle(
        PlaceOrderCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Orders
            .Include(x => x.Lines)
            .Include(x => x.Fees)
            .Where(new OrderByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Order {request.Id} not found");

        entity.Place(clock.UtcNow);

        await inventoryService.DecrementForOrderAsync(
            entity.Id,
            entity.LocationId,
            entity.Lines
                .Select(x => new StockLine(x.ProductId, x.Id, x.Quantity))
                .ToList(),
            request.PlacedByUserId,
            cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await CompensateStockAsync(entity.Id, request.PlacedByUserId);

            throw;
        }

        await PublishOrderPlacedIntegrationEventAsync(entity, cancellationToken);

        return Result.Success();
    }

    // Best-effort: a failed compensation is logged, and the original save failure still surfaces.
    private async Task CompensateStockAsync(
        long orderId,
        string performedByUserId)
    {
        try
        {
            await inventoryService.RestoreForOrderAsync(orderId, performedByUserId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to restore stock for order {OrderId} after its placement could not be saved.",
                orderId);
        }
    }

    // A downstream subscriber fault must never fail the placement call, mirrors
    // ApprovalService.PublishFinalizedIntegrationEventAsync.
    private async Task PublishOrderPlacedIntegrationEventAsync(
        Order entity,
        CancellationToken cancellationToken)
    {
        try
        {
            await publisher.Publish(
                new OrderPlacedIntegrationEvent(
                    entity.Id,
                    entity.LocationId,
                    entity.PlacedAt!.Value,
                    entity.Lines
                        .Select(x => new OrderLineSnapshot(
                            x.ProductId,
                            x.Sku,
                            x.Quantity,
                            x.UnitPrice.Amount,
                            x.RequestedSalePrice?.Amount))
                        .ToList()),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to publish {IntegrationEvent} for order {OrderId}.",
                nameof(OrderPlacedIntegrationEvent),
                entity.Id);
        }
    }
}
