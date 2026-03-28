using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monolith.Identity.Jwt;
using Monolith.Modularity;

namespace Monolith.Identity;

public class IdentityModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityServices(configuration);

        services.AddJwtAuthentication(configuration);

        ShowModuleInfo();
    }
}
