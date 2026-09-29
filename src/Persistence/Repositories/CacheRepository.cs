using Light.Extensions.Caching;
using Microsoft.Extensions.Logging;

namespace StarterKit.Persistence.Repositories;

public class CacheRepository<TEntity, TContext>(
    TContext context,
    ICacheService cacheService,
    ILogger<CacheRepositoryBase<TEntity>> logger)
    : CacheRepositoryBase<TEntity>(context, cacheService, logger),
    ICacheRepository<TEntity, TContext>
    where TEntity : class
    where TContext : DbContext;