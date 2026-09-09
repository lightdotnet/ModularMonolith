using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Infrastructure.Modularity;
using StarterKit.Notifications.Contracts.SystemNotifications;

namespace StarterKit.Notifications.Api.SignalR;

public class SignalRModule : AppModule
{
    public override void Add(IServiceCollection services)
    {
        services.AddSignalR();

        /* use only for Services API */
        services.AddSingleton<IUserIdProvider, CustomIdProvider>();

        services.AddScoped<SignalRHub>();

        services.AddScoped<IHubService, HubService>();

        ShowModuleInfo();
    }
}

public class SignalREndpoint : AppModuleEndpoint
{
    public override void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<SignalRHub>(NotificationConstants.HubPath, options =>
        {
            options.CloseOnAuthenticationExpiration = true;
            options.Transports = HttpTransportType.WebSockets;
        });
    }
}