using Light.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Infrastructure.Modularity;
using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Services;
using StarterKit.Organization.Contracts.Authorization;
using StarterKit.Organization.Contracts.Services;
using StarterKit.Persistence;

namespace StarterKit.Organization.Api;

public class OrganizationModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddConfiguredDbContext<OrganizationDbContext>(
            configuration,
            DbConnectionNames.Organization);

        services.AddScoped<IOrgDirectoryService, OrgDirectoryService>();

        services.AddSingleton<IPermissionDefinitionProvider, OrganizationPermissionProvider>();

        ShowModuleInfo();
    }
}
