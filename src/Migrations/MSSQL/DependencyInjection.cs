using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.EventBusMassTransitRabbitMQ;
using StarterKit.Infrastructure;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.IntegrationEvents;
using StarterKit.Persistence;
using StarterKit.Persistence.MigrationSupport;
using System.Reflection;

namespace MSSQL;

public static class DependencyInjection
{
    public static IServiceCollection AddMigrator(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSharedInfrastructure();

        services.AddMigrationsServices();

        // No RabbitMQ section in the migrator's configuration, so this registers the no-op bus.
        services.AddEventBus(configuration);

        services.AddIdentity(configuration);

        return services;
    }

    private static IServiceCollection AddIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DbConnectionNames.Identity);

        services.AddDbContext<IdentityDbContext>(options =>
            options
                .UseSqlServer(connectionString, o =>
                {
                    o.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                })
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services
            .AddIdentityCore<User>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false;

                // Password settings
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 3;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;

                // Lockout settings
                //options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(1);
                //options.Lockout.MaxFailedAccessAttempts = 10;

                // User settings
                options.User.RequireUniqueEmail = false;
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<IdentityDbContext>();

        // IdentityDbContext buffers integration events per scope and publishes them after a save.
        services.AddScoped<IntegrationEventCollector>();

        services.AddScoped<IdentityContextInitialiser>();

        return services;
    }
}
