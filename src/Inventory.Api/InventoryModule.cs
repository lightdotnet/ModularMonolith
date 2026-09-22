using Light.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Infrastructure.Modularity;
using StarterKit.Inventory.Api.Data;
using StarterKit.Inventory.Api.Services;
using StarterKit.Inventory.Contracts.Authorization;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Persistence;

namespace StarterKit.Inventory.Api;

public class InventoryModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddConfiguredDbContext<InventoryDbContext>(
            configuration,
            DbConnectionNames.Inventory);

        services.AddScoped<StockLedger>();

        services.AddScoped<IInventoryService, InventoryService>();

        services.AddSingleton<IPermissionDefinitionProvider, InventoryPermissionProvider>();

        ShowModuleInfo();
    }
}
