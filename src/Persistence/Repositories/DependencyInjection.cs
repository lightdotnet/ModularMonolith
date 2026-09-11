namespace StarterKit.Persistence.Repositories;

public static class DependencyInjection
{
    /// <summary>
    ///     Registers <see cref="ICacheRepository{TEntity, TContext}"/>. Assumes the cache provider
    ///     (<c>AddAppCache</c> in <c>StarterKit.Infrastructure.Caching.DependencyInjection</c>) is
    ///     already registered by the composition root — call this after <c>AddAppCache</c>, not
    ///     instead of it. This method does not register a cache provider itself.
    /// </summary>
    public static IServiceCollection AddCachingServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(ICacheRepository<,>), typeof(CacheRepository<,>));

        return services;
    }
}
