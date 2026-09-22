using Light.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Infrastructure.Modularity;
using StarterKit.Orders.Api.Application.Orders;
using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Services;
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

        services.AddScoped<IOrderTypeCache, OrderTypeCache>();

        services.AddSingleton<IPermissionDefinitionProvider, OrdersPermissionProvider>();

        services.AddOptions<OrphanedStockReconciliationOptions>()
            .BindConfiguration("Orders:StockReconciliation")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHostedService<OrphanedStockReconciliationService>();

        ShowModuleInfo();
    }
}
