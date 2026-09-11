using Light.Extensions.Caching;
using Microsoft.Extensions.Logging;

namespace StarterKit.Persistence.Repositories
{
    internal class DynamicsDbCache<T, TContext>(
        TContext context,
        ICacheService cacheService,
        ILogger<CacheRepositoryBase<T>> logger)
        : CacheRepository<T, TContext>(context, cacheService, logger), IDynamicsDbCache<T>
        where T : class
        where TContext : DbContext
    {
    }
}
