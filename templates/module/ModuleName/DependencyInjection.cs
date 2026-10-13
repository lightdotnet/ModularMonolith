using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StarterKit.Modules.ModuleName;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the ModuleName module's services. The use cases are mediator handlers, picked
    /// up by the host's mediator assembly scan.
    /// </summary>
    public static IServiceCollection AddModuleNameServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }
}
