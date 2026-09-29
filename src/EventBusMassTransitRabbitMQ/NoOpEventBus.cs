using Light.EventBus.Abstractions;
using Light.EventBus.Events;
using Microsoft.Extensions.Logging;

namespace StarterKit.EventBusMassTransitRabbitMQ;

/// <summary>
/// <see cref="IEventBus"/> used when the event bus is disabled: logs and drops every event.
/// </summary>
public class NoOpEventBus(ILogger<NoOpEventBus> logger)
    : IEventBus
{
    public Task Publish<T>(
        T message,
        CancellationToken cancellationToken = default)
        where T : IIntegrationEvent
    {
        logger.LogInformation(
            "event_bus {EventName} {Id} not published: event bus is disabled",
            typeof(T).Name,
            message.Id);

        return Task.CompletedTask;
    }
}
