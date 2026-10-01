using Light.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Modules.Identity.Api;
using StarterKit.Modules.Identity.Application.Authentication.ExternalLogin;
using StarterKit.Modules.Identity.Application.Authorization;
using StarterKit.Modules.Identity.Application.Common;
using StarterKit.Modules.Identity.Application.Roles.Services;
using StarterKit.Modules.Identity.Application.Users.Services;
using StarterKit.Modules.Identity.Contracts;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.Infrastructure.Authentication;
using StarterKit.Modules.Identity.Infrastructure.Authentication.ExternalLogin;
using StarterKit.Modules.Identity.Infrastructure.Persistence;
using StarterKit.Persistence;
using System.Runtime.InteropServices;

namespace StarterKit.Modules.Identity;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Identity store (ASP.NET Core Identity over <c>IdentityDbContext</c>), Active
    /// Directory, the user/role/external-login services, the integration-event collector and the
    /// <see cref="IIdentityModuleApi"/> seam. JWT issuance is registered separately by
    /// <see cref="IdentityModule"/>; the authentication scheme is the host's concern.
    /// </summary>
    public static IdentityBuilder AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
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
            .AddClaimsPrincipalFactory<IdentityClaimsPrincipalFactory>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        services.AddOptions<ExternalLoginOptions>().BindConfiguration("Authentication:Microsoft");

        services.AddScoped<IntegrationEventCollector>();

        // UserService is decorated so every successful user write reloads the cached user list;
        // ReloadUserCacheHandler is registered by the host's mediator assembly scan.
        services.AddTransient<UserService>();
        services.AddTransient<IUserService, UserCacheReloadingUserService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddTransient<IRoleService, RoleService>();
        services.AddScoped<PermissionGrantGuard>();
        services.AddScoped<IExternalLoginService, ExternalLoginService>();
        services.AddScoped<IExternalLoginAuthCodeStore, ExternalLoginAuthCodeStore>();
        services.AddScoped<IIdentityModuleApi, IdentityModuleApi>();

        return identityBuilder;
    }

    private static void AddActiveDirectory(
        IServiceCollection services,
        IConfiguration configuration)
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
