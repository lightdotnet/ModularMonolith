using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.EventBusMassTransitRabbitMQ;
using StarterKit.Infrastructure.Modularity;

namespace StarterKit.Modules.ModuleName;

public class ModuleNameModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleNameServices(configuration);

        ShowModuleInfo();
    }
}

/// <summary>
/// Registers the ModuleName module's integration-event consumers with the event bus. Picked
/// up by the assembly scan in <c>AddEventBus</c>; unused when the event bus is disabled.
/// </summary>
public sealed class ModuleNameModuleConsumer : AppModuleConsumer
{
    public override void AddConsumers(IBusRegistrationConfigurator configurator)
    {
    }
}
