using Microsoft.Extensions.Logging;
using Monolith.Catalog.Domain.Shops;

namespace Monolith.Catalog.UseCases.Shops.EventHandlers;

internal class ShopStatusUpdatedEventHandler(
    ILogger<ShopStatusUpdatedEventHandler> logger)
    : INotificationHandler<ShopStatusUpdatedEvent>
{
    public Task Handle(ShopStatusUpdatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Shop status updated: Shop ID: {ShopId}, New Status: {NewStatus}",
            notification.Name, notification.Status);

        // Additional logic can be added here, such as notifying other services or updating related data.

        return Task.CompletedTask;
    }
}
