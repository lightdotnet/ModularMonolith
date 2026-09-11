using Light.Extensions.DependencyInjection;
using Light.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace StarterKit.Infrastructure.Caching;

public static class DependencyInjection
{
    public static IServiceCollection AddAppCache(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("Caching").Get<CacheOptions>() ?? new CacheOptions();

        services.AddCache(opt =>
        {
            opt.Provider = settings.Provider;
            opt.RedisHost = settings.RedisHost;
            opt.RedisPassword = settings.RedisPassword;
        });

        return services;
    }
}
