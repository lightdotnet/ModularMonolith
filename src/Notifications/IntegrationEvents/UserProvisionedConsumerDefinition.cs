using StarterKit.EventBusMassTransitRabbitMQ;
using StarterKit.Modules.Identity.Contracts.IntegrationEvents;

namespace StarterKit.Modules.Notifications.IntegrationEvents;

internal sealed class UserProvisionedConsumerDefinition()
    : AppConsumerDefinition<UserProvisionedIntegrationEvent, UserProvisionedConsumer>(NotificationsModuleConsumer.EndpointNamePrefix);
