using Light.MassTransit.RabbitMQ;
using Microsoft.Extensions.Logging;
using StarterKit.Shared;

namespace StarterKit.EventBusMassTransitRabbitMQ;

public abstract class AppConsumer<TMessage>(ILogger logger)
    : Consumer<TMessage>(logger)
    where TMessage : IntegrationEvent;