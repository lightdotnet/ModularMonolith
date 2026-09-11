using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Identity.Api.Jwt;
using StarterKit.Infrastructure.Modularity;

namespace StarterKit.Identity.Api;

public class IdentityModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityServices(configuration);

        services.AddJwtTokenServices(configuration);

        services.AddSingleton<IPermissionDefinitionProvider, IdentityPermissionProvider>();

        ShowModuleInfo();
    }
}
