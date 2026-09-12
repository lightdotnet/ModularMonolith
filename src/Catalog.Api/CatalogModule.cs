using Light.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Catalog.Api.Data;
using StarterKit.Catalog.Api.Services;
using StarterKit.Catalog.Contracts.Authorization;
using StarterKit.Catalog.Contracts.Services;
using StarterKit.Infrastructure.Modularity;
using StarterKit.Persistence;

namespace StarterKit.Catalog.Api;

public class CatalogModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddConfiguredDbContext<CatalogDbContext>(
            configuration,
            DbConnectionNames.Catalog);

        services.AddScoped<ICatalogPricingService, CatalogPricingService>();

        services.AddSingleton<IPermissionDefinitionProvider, CatalogPermissionProvider>();

        ShowModuleInfo();
    }
}
