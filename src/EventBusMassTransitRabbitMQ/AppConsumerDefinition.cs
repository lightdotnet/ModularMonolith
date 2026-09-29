using MassTransit;
using StarterKit.Shared;

namespace StarterKit.EventBusMassTransitRabbitMQ;

public abstract class AppConsumerDefinition<TEvent, TConsumer>
    : Light.MassTransit.RabbitMQ.ConsumerDefinition<TEvent, TConsumer>
    where TEvent : IntegrationEvent
    where TConsumer : AppConsumer<TEvent>
{
    protected AppConsumerDefinition()
        : this(null)
    {
    }

    /// <summary>
    /// Creates the definition with a per-module queue name prefix, giving each module its own queue
    /// so modules consuming the same event don't compete for it.
    /// </summary>
    protected AppConsumerDefinition(string? endpointNamePrefix)
        : base(endpointNamePrefix)
    {
        // limit the number of messages consumed concurrently
        // this applies to the consumer only, not the endpoint
        ConcurrentMessageLimit = 10;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator configurator,
        IConsumerConfigurator<TConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // short in-memory retries for transient failures;
        // once exhausted the message moves to the _error queue
        configurator.UseMessageRetry(r => r.Intervals(
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15)));

        // hold messages published by the consumer until it completes successfully,
        // so a failed/retried attempt publishes nothing
        configurator.UseInMemoryOutbox(context);
    }
}
