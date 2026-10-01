using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Infrastructure.Modularity;
using StarterKit.Modules.Identity.Infrastructure.Authentication.Jwt;

namespace StarterKit.Modules.Identity;

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
