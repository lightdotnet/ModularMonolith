using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.EventBusMassTransitRabbitMQ;
using StarterKit.Infrastructure.Modularity;
using StarterKit.Modules.Notifications.Application.Users.IntegrationEvents.Consumers;

namespace StarterKit.Modules.Notifications;

public class NotificationsModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddNotificationsServices(configuration);

        services.AddSmtpMail(configuration);

        services.AddSingleton<IPermissionDefinitionProvider, NotificationPermissionProvider>();

        ShowModuleInfo();
    }
}

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
