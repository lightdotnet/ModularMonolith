using MassTransit;
using StarterKit.EventBusMassTransitRabbitMQ;

namespace StarterKit.Modules.Notifications.IntegrationEvents;

/// <summary>
/// Registers the Notifications module's integration-event consumers with the event bus. Picked
/// up by the assembly scan in <c>AddEventBus</c>; unused when the event bus is disabled.
/// </summary>
public sealed class NotificationsModuleConsumer : AppModuleConsumer
{
    /// <summary>
    /// Queue name prefix giving this module its own queue per event. It only applies to events
    /// that carry a binding name; otherwise the queue is named after the consumer type, which is
    /// already specific to this module.
    /// </summary>
    internal const string EndpointNamePrefix = "notifications";

    public override void AddConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<UserProvisionedConsumer, UserProvisionedConsumerDefinition>();
    }
}
