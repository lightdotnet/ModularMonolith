using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Modules.Identity.Application.Authentication;

namespace StarterKit.Modules.Identity.Infrastructure.Authentication.Jwt;

internal static class JwtServiceCollectionExtensions
{
    public static void AddJwtTokenServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        const string sectionName = "Jwt";
        services.AddOptions<JwtOptions>().BindConfiguration(sectionName);
        var jwtSettings = configuration.GetSection(sectionName).Get<JwtOptions>();
        _ = jwtSettings ?? throw new InvalidOperationException("Configuration section 'Jwt' is missing or invalid.");

        services.AddSingleton<JwtSigningService>();
        services.AddScoped<JwtTokenIssuer>();
        services.AddScoped<HubTokenIssuer>();
        services.AddScoped<IUserSessionService, UserSessionService>();
        services.AddTransient<IAuthenticationService, AuthenticationService>();
    }
}
