using Microsoft.Extensions.Logging;
using StarterKit.Shared;
using System.Reflection;

namespace StarterKit.Persistence.MigrationSupport;

public static class MigrationsExtensions
{
    public static IServiceCollection AddMigrationsServices(this IServiceCollection services)
    {
        services.AddSingleton<ICurrentUser, MigratorCurrentUser>();
        services.AddMediatorFromAssemblies(Assembly.GetExecutingAssembly());

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
