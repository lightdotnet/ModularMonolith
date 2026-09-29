using Light.EventBus.Abstractions;
using Light.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Shared;
using System.Reflection;

namespace StarterKit.EventBusMassTransitRabbitMQ;

public static class Startup
{
    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies)
    {
        var settings = configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>();

        if (settings is null || !settings.Enable)
        {
            services.AddSingleton<IEventBus, NoOpEventBus>();
            return services;
        }

        // Host/Username/Password are validated by AddRabbitMQEventBus
        services.AddRabbitMQEventBus(x =>
        {
            x.AddConsumers(assemblies);
            x.ConfigRabbitMQ(mq =>
            {
                mq.Host = settings.Host;
                mq.Username = settings.Username;
                mq.Password = settings.Password;
                mq.Exclude<IntegrationEvent>();
            });
        });

        return services;
    }
}
