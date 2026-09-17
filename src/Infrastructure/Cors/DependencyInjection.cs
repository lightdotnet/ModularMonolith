using Light.AspNetCore.Cors;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace StarterKit.Infrastructure.Cors;

public static class DependencyInjection
{
    private const string CorsPolicyName = "AllowCors";

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("CorsOrigins").Get<string[]?>();
        if (origins is not null)
        {
            services.AddCors(opts => opts.AllowOrigins(CorsPolicyName, origins));
        }
        return services;
    }

    public static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app)
    {
        return app.UseCors(CorsPolicyName);
    }
}
