using Microsoft.Extensions.Logging;
using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Orders.Contracts.Events;
using StarterKit.Shared;

namespace StarterKit.Orders.Api.Application.Orders.Commands;

internal sealed record PlaceOrderCommand(long Id) : ICommand<IResult>;

internal sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal class PlaceOrderCommandHandler(
    OrdersDbContext context,
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

        await context.SaveChangesAsync(cancellationToken);

        await PublishOrderPlacedIntegrationEventAsync(entity, cancellationToken);

        return Result.Success();
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
