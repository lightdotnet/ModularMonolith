using Light.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Identity.Api.Data;
using StarterKit.Identity.Api.Entities;
using StarterKit.Identity.Api.Services;
using StarterKit.Identity.Contracts.ExternalLogin;
using StarterKit.Identity.Contracts.Services;
using StarterKit.Persistence;
using System.Runtime.InteropServices;

namespace StarterKit.Identity.Api;

public static class DependencyInjection
{
    public static IdentityBuilder AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        AddActiveDirectory(services, configuration);

        services.AddConfiguredDbContext<IdentityDbContext>(
            configuration,
            DbConnectionNames.Identity);

        var identityBuilder = services
            .AddIdentityCore<User>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false;

                // Password settings
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;

                // Lockout settings
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                //options.User.RequireUniqueEmail = true;
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        services.AddOptions<ExternalLoginOptions>().BindConfiguration("Authentication:Microsoft");

        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IRoleService, RoleService>();
        services.AddScoped<IExternalLoginService, ExternalLoginService>();
        //services.AddTransient<IServiceClaimService, ServiceClaimService>();

        return identityBuilder;
    }

    private static void AddActiveDirectory(IServiceCollection services, IConfiguration configuration)
    {
        // connect to AD
        var domainName = configuration.GetValue<string>("MemberOfDomain");
        if (!string.IsNullOrEmpty(domainName)
            && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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