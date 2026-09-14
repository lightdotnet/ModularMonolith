using Light.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Infrastructure.Modularity;
using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Contracts.Authorization;
using StarterKit.Persistence;

namespace StarterKit.Orders.Api;

public class OrdersModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddConfiguredDbContext<OrdersDbContext>(
            configuration,
            DbConnectionNames.Orders);

        services.AddSingleton<IPermissionDefinitionProvider, OrdersPermissionProvider>();

        ShowModuleInfo();
    }
}
