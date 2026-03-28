using Light.AspNetCore.Cors;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Monolith.CORS;

public static class DependencyInjection
{
    private const string CORS_POLICY_NAME = "AllowCors";

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("CorsOrigins").Get<string[]?>();
        if (origins is not null)
        {
            services.AddCors(opts => opts.AllowOrigins(CORS_POLICY_NAME, origins));
        }
        return services;
    }

    public static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app)
    {
        return app.UseCors(CORS_POLICY_NAME);
    }
}
