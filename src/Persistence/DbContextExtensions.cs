using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace StarterKit.Persistence;

public static class DbContextExtensions
{
    public static DbProvider GetDbProvider(this IConfiguration configuration) =>
        configuration.GetValue<DbProvider>("DbProvider");

    internal static DbContextOptionsBuilder ConfigureDatabase(this DbContextOptionsBuilder builder, DbProvider dbProvider, string connectionString)
    {
        builder
            .ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning));

        return dbProvider switch
        {
            DbProvider.PostgreSQL =>
                builder.UseNpgsql(connectionString).EnableSensitiveDataLogging(),
            DbProvider.MSSQL =>
                builder.UseSqlServer(connectionString),
            DbProvider.Sqlite =>
                builder.UseSqlite(connectionString),
            _ => throw new InvalidOperationException($"DB Provider {dbProvider} is not supported."),
        };
    }

    public static IServiceCollection AddConfiguredDbContext<TContext>(this IServiceCollection services, IConfiguration configuration, string connectionName)
        where TContext : DbContext
    {
        var dbProvider = configuration.GetDbProvider();

        if (dbProvider == DbProvider.InMemory)
        {
            services.AddDbContext<TContext>(options => options.UseInMemoryDatabase($"InMemoryDb"));
        }
        else
        {
            var connectionString = configuration.GetConnectionString(connectionName)
                ?? throw new InvalidOperationException($"{connectionName} connection string is not configured.");

            services.AddDbContext<TContext>(options =>
            {
                options.ConfigureDatabase(dbProvider, connectionString);
            });
        }

        return services;
    }
}
