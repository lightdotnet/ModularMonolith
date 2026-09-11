using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Approval.Api.Data;
using StarterKit.Identity.Api.Entities;
using StarterKit.Infrastructure;
using StarterKit.LeaveManagement.Api.Data;
using StarterKit.Notifications.Api.Data;
using StarterKit.Organization.Api.Data;
using StarterKit.Persistence;
using StarterKit.Persistence.MigrationSupport;
using System.Reflection;

namespace MSSQL;

public static class DependencyInjection
{
    public static IServiceCollection AddMigrator(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSharedInfrastructure();

        services.AddMigrationsServices();

        services.AddIdentity(configuration);

        services.AddNotification(configuration);

        services.AddOrganization(configuration);

        services.AddApproval(configuration);

        services.AddLeaveManagement(configuration);

        return services;
    }

    private static IServiceCollection AddIdentity(this IServiceCollection services, IConfiguration configuration)
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

        services.AddScoped<IdentityContextInitialiser>();

        return services;
    }

    private static void AddNotification(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DbConnectionNames.Identity);

        services.AddDbContext<NotificationDbContext>(options =>
            options
                .UseSqlServer(connectionString, o =>
                {
                    o.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                })
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<NotificationContextInitialiser>();
    }

    private static void AddOrganization(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DbConnectionNames.Organization);

        services.AddDbContext<OrganizationDbContext>(options =>
            options
                .UseSqlServer(connectionString, o =>
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
                .UseSqlServer(connectionString, o =>
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
                .UseSqlServer(connectionString, o =>
                {
                    o.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                })
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<LeaveManagementContextInitialiser>();
    }
}