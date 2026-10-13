using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Persistence;

namespace StarterKit.Modules.ModuleName.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the module's store (<see cref="ApplicationContext"/>, exposed to the use cases as
    /// <see cref="IApplicationContext"/>). The use cases are mediator handlers, picked up by the
    /// host's mediator assembly scan.
    /// </summary>
    public static IServiceCollection AddModuleNameServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddConfiguredDbContext<ApplicationContext>(
            configuration,
            DbConnectionNames.Default);

        services.AddScoped<IApplicationContext>(sp => sp.GetRequiredService<ApplicationContext>());

        return services;
    }
}
