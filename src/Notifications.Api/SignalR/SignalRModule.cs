using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StarterKit.Infrastructure.Modularity;
using StarterKit.Notifications.Contracts.SystemNotifications;

namespace StarterKit.Notifications.Api.SignalR;

public class SignalRModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSignalR();

        /* use only for Services API */
        services.AddSingleton<IUserIdProvider, CustomIdProvider>();

        services.AddScoped<SignalRHub>();

        services.AddScoped<IHubService, HubService>();

        services.AddNotificationHubOptions(configuration);

        ShowModuleInfo();
    }
}

public class SignalREndpoint : AppModuleEndpoint
{
    public override void Map(IEndpointRouteBuilder endpoints)
    {
        var hubPath = endpoints.ServiceProvider
            .GetRequiredService<IOptions<NotificationHubOptions>>().Value.Path;

        endpoints.MapHub<SignalRHub>(hubPath, options =>
        {
            options.CloseOnAuthenticationExpiration = true;
            options.Transports = HttpTransportType.WebSockets;
        });
    }
}
