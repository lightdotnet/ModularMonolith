using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Approval.Api.Data;
using StarterKit.Catalog.Api.Data;
using StarterKit.Currencies.Api.Data;
using StarterKit.Identity.Api.Entities;
using StarterKit.Infrastructure;
using StarterKit.Inventory.Api.Data;
using StarterKit.LeaveManagement.Api.Data;
using StarterKit.Locations.Api.Data;
using StarterKit.Orders.Api.Data;
using StarterKit.Organization.Api.Data;
using StarterKit.Persistence;
using StarterKit.Persistence.MigrationSupport;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Transfers.Api.Data;
using System.Reflection;

namespace PostgreSQL;

public static class DependencyInjection
{
    public static IServiceCollection AddMigrator(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSharedInfrastructure();

        services.AddMigrationsServices();

        services.AddIdentity(configuration);

        services.AddOrganization(configuration);

        services.AddApproval(configuration);

        services.AddLeaveManagement(configuration);

        services.AddLocation(configuration);

        services.AddCatalog(configuration);

        services.AddOrders(configuration);

        services.AddInventory(configuration);

        services.AddTransfers(configuration);

        services.AddPurchasing(configuration);

        services.AddCurrency(configuration);

        return services;
    }

    private static IServiceCollection AddIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DbConnectionNames.Identity);

        services.AddDbContext<IdentityDbContext>(options =>
            options
                .UseNpgsql(connectionString, o =>
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

        services.AddScoped<IdentityContextInitialiser>();

        return services;
    }

    private static void AddOrganization(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DbConnectionNames.Organization);

        services.AddDbContext<OrganizationDbContext>(options =>
            options
                .UseNpgsql(connectionString, o =>
                {
                    o.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                })
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<OrganizationContextInitialiser>();
    }

    private static void AddApproval(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DbConnectionNames.Approval);

        services.AddDbContext<ApprovalDbContext>(options =>
            options
                .UseNpgsql(connectionString, o =>
                {
                    o.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                })
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<ApprovalContextInitialiser>();
    }

    private static void AddLeaveManagement(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DbConnectionNames.LeaveManagement);

        services.AddDbContext<LeaveManagementDbContext>(options =>
            options
                .UseNpgsql(connectionString, o =>
                {
                    o.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                })
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<LeaveManagementContextInitialiser>();
    }

    private static void AddLocation(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DbConnectionNames.Location);

        services.AddDbContext<LocationDbContext>(options =>
            options
                .UseNpgsql(connectionString, o =>
                {
                    o.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                })
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<LocationContextInitialiser>();
    }

    private static void AddCatalog(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DbConnectionNames.Catalog);

        services.AddDbContext<CatalogDbContext>(options =>
            options
                .UseNpgsql(connectionString, o =>
                {
                    o.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                })
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<CatalogContextInitialiser>();
    }

    private static void AddOrders(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DbConnectionNames.Orders);

        services.AddDbContext<OrdersDbContext>(options =>
            options
                .UseNpgsql(connectionString, o =>
                {
                    o.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                })
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<OrdersContextInitialiser>();
    }

    private static void AddInventory(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DbConnectionNames.Inventory);

        services.AddDbContext<InventoryDbContext>(options =>
            options
                .UseNpgsql(connectionString, o =>
                {
                    o.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                })
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<InventoryContextInitialiser>();
    }

    private static void AddTransfers(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DbConnectionNames.Transfers);

        services.AddDbContext<TransfersDbContext>(options =>
            options
                .UseNpgsql(connectionString, o =>
                {
                    o.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                })
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<TransfersContextInitialiser>();
    }

    private static void AddPurchasing(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DbConnectionNames.Purchasing);

        services.AddDbContext<PurchasingDbContext>(options =>
            options
                .UseNpgsql(connectionString, o =>
                {
                    o.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                })
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<PurchasingContextInitialiser>();
    }

    private static void AddCurrency(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DbConnectionNames.Currency);

        services.AddDbContext<CurrencyDbContext>(options =>
            options
                .UseNpgsql(connectionString, o =>
                {
                    o.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                })
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<CurrencyContextInitialiser>();
    }
}
