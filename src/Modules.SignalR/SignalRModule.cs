using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Monolith.Modularity;

namespace Monolith.SignalR;

public class SignalRModule : AppModule
{
    public override void Add(IServiceCollection services)
    {
        services.AddSignalR();

        /* use only for Services API */
        services.AddSingleton<IUserIdProvider, CustomIdProvider>();

        services.AddScoped<SignalRHub>();

        services.AddScoped<IHubService, HubService>();
    }
}

public class SignalREndpoint : AppHub
{
    public override void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<SignalRHub>("/signalr-hub", options =>
        {
            options.CloseOnAuthenticationExpiration = true;
            options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
        });
    }
}