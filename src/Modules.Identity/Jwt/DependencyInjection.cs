using Light.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monolith.Claims;

namespace Monolith.Identity.Jwt;

public static class DependencyInjection
{
    public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Override by BindConfiguration
        var sectionName = "Jwt";
        services.AddOptions<JwtOptions>().BindConfiguration(sectionName);
        var jwtSettings = configuration.GetSection(sectionName).Get<JwtOptions>();
        ArgumentNullException.ThrowIfNull(jwtSettings, nameof(JwtOptions));

        // add JWT Auth
        services.AddJwtAuth(jwtSettings.Issuer, jwtSettings.SecretKey, ClaimTypes.Role); // inject this for use jwt auth

        // services
        services.AddScoped<JwtTokenMananger>();
        services.AddTransient<ITokenService, TokenService>();
    }
}