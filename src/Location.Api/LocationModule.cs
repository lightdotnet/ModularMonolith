using Light.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Infrastructure.Modularity;
using StarterKit.Locations.Api.Data;
using StarterKit.Locations.Api.Services;
using StarterKit.Locations.Contracts.Authorization;
using StarterKit.Locations.Contracts.Services;
using StarterKit.Persistence;

namespace StarterKit.Locations.Api;

public class LocationModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddConfiguredDbContext<LocationDbContext>(
            configuration,
            DbConnectionNames.Location);

        services.AddScoped<ILocationDirectoryService, LocationDirectoryService>();

        services.AddScoped<ILocationTypeCache, LocationTypeCache>();

        services.AddSingleton<IPermissionDefinitionProvider, LocationPermissionProvider>();

        ShowModuleInfo();
    }
}
