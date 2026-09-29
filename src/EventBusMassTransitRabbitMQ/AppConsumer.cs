using Light.EventBus.Events;
using Light.MassTransit.RabbitMQ;
using Microsoft.Extensions.Logging;

namespace StarterKit.EventBusMassTransitRabbitMQ;

public abstract class AppConsumer<TMessage>(ILogger logger)
    : Consumer<TMessage>(logger)
    where TMessage : class, IIntegrationEvent;