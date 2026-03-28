using Light.ActiveDirectory;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monolith.Database;
using Monolith.Identity.Application.Notifications;
using Monolith.Identity.Data;
using Monolith.Identity.Services;
using System.Runtime.InteropServices;

namespace Monolith.Identity;

public static class DependencyInjection
{
    public static IdentityBuilder AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        AddActiveDirectory(services, configuration);

        services.AddDbContext<IdentityDbContext>(configuration, DbConnectionNames.IDENTITY);

        var identityBuilder = services
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
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IRoleService, RoleService>();

        services.AddTransient<INotificationService, NotificationService>();

        return identityBuilder;
    }

    private static void AddActiveDirectory(IServiceCollection services, IConfiguration configuration)
    {
        // connect to AD
        var domainName = configuration.GetValue<string>("MemberOfDomain");
        if (!string.IsNullOrEmpty(domainName) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddActiveDirectory(x => x.Name = domainName);
        }
        else
        {
            // fake service
            services.AddActiveDirectory();
        }
    }
}