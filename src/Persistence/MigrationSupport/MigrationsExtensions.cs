using Microsoft.Extensions.Logging;
using StarterKit.Shared;
using System.Reflection;

namespace StarterKit.Persistence.MigrationSupport;

public static class MigrationsExtensions
{
    /// <summary>
    /// Registers the migrator's current user and the mediator, scanning the Persistence assembly plus
    /// <paramref name="moduleAssemblies"/> so module handlers invoked while seeding (for example
    /// domain-event handlers dispatched on save) are registered.
    /// </summary>
    public static IServiceCollection AddMigrationsServices(
        this IServiceCollection services,
        params Assembly[] moduleAssemblies)
    {
        services.AddSingleton<ICurrentUser, MigratorCurrentUser>();
        services.AddMediatorFromAssemblies([Assembly.GetExecutingAssembly(), .. moduleAssemblies]);

        return services;
    }

    public static async Task MigrateDatabaseAsync<TContext>(this TContext context, ILogger logger)
        where TContext : DbContext
    {
        var dbName = context.Database.GetDbConnection().Database;

        logger.LogInformation("database {name} initializing ...", dbName);

        try
        {
            if (context.Database.GetMigrations().Any())
            {
                if ((await context.Database.GetPendingMigrationsAsync()).Any())
                {
                    await context.Database.MigrateAsync();

                    logger.LogInformation("database {name} initialized", dbName);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }
}
