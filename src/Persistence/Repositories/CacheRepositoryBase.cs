using Light.EntityFrameworkCore.Repositories;
using Light.Extensions.Caching;
using Microsoft.Extensions.Logging;

namespace StarterKit.Persistence.Repositories;

/// <summary>
///     Scope: opt-in only, for small, infrequently-written reference/lookup tables that benefit
///     from a whole-table in-memory cache (e.g. a type catalog). This is NOT a general replacement
///     for this solution's default direct-DbContext-plus-Specification access pattern used by every
///     module — most entities should keep using that pattern.
/// </summary>
/// <remarks>
///     <para>
///         Cached entities are read-only snapshots: <see cref="RefreshCacheAsync"/> queries with
///         <c>AsNoTracking()</c>, so instances handed back from cache are detached and must never be
///         mutated in place. Callers that need to change an entity must re-fetch a tracked instance
///         from the <see cref="DbContext"/> (or via this repository's write members) before mutating.
///     </para>
///     <para>
///         <b>Write-path contract (unenforced):</b> any entity type cached through this repository
///         MUST be written exclusively through this repository — never call
///         <c>SaveChanges</c>/<c>SaveChangesAsync</c> directly on the underlying <see cref="DbContext"/>
///         for that entity type, or the cache will silently go stale with no error. This is a real,
///         unenforced constraint — audit call sites by hand if you adopt this for a new entity.
///     </para>
/// </remarks>
public abstract class CacheRepositoryBase<T>(
    DbContext context,
    ICacheService cacheService,
    ILogger<CacheRepositoryBase<T>> logger) : Repository<T>(context), ICacheRepository<T>
    where T : class
{
    private readonly ICacheService _cacheService = cacheService;
    private readonly DbContext _context = context;
    private readonly ILogger<CacheRepositoryBase<T>> _logger = logger;

    /// <summary>
    ///     Requires a relational EF Core provider — <c>GetDbConnection()</c> throws against the
    ///     <c>InMemory</c> provider. Every member of this class reads <see cref="CacheKey"/>, so this
    ///     repository cannot be used with a <see cref="DbContext"/> configured on the InMemory
    ///     provider at all (tests exercise it against Sqlite instead).
    /// </summary>
    public virtual string CacheKey
    {
        get
        {
            var databaseName = _context.Database.GetDbConnection().Database;
            return $"[{databaseName}]_[{typeof(T).FullName}]";
        }
    }

    public async Task<IReadOnlyList<T>> RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        var key = CacheKey;

        await TryRemoveCacheKeyAsync(key, cancellationToken);

        // Cached entities are shared through a singleton cache while the DbContext is
        // scoped-per-request — query with no tracking so cached instances are read-only
        // snapshots, never live tracked entities attached to a caller's DbContext.
        var data = await _context.Set<T>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var lifeTime = TimeSpan.FromMinutes(35);

        await _cacheService.TrySetAsync(key, data, lifeTime, cancellationToken: cancellationToken);

        return data;
    }

    public override async Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default)
    {
        var key = CacheKey;

        var data = await _cacheService.TryGetAsync<IReadOnlyList<T>>(key, cancellationToken);

        data ??= await RefreshCacheAsync(cancellationToken);

        return data;
    }

    public int SaveChanges()
    {
        var result = _context.SaveChanges();

        RefreshCache();

        return result;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _context.SaveChangesAsync(cancellationToken);
        await RefreshCacheAsync(cancellationToken: cancellationToken);
        return result;
    }

    public virtual async Task RemoveCacheAsync(CancellationToken cancellationToken = default)
    {
        var key = CacheKey;
        await _cacheService.RemoveAsync(key, cancellationToken);
    }

    /// <summary>
    ///     Removes a cache key the same way <see cref="ICacheService"/>'s own <c>Try*</c> members do:
    ///     swallow any backend failure and log it, rather than throw. <see cref="ICacheService"/> has
    ///     no <c>TryRemoveAsync</c>, so this wraps the non-<c>Try</c> <see cref="ICacheService.RemoveAsync"/>
    ///     by hand. This is used only where a cache-backend hiccup must not surface as a failure of an
    ///     already-committed DB write (see <see cref="RefreshCacheAsync"/>) — it is not a substitute for
    ///     <see cref="RemoveCacheAsync"/>, where an explicit invalidation request failing is expected to
    ///     be visible to the caller.
    /// </summary>
    private async Task TryRemoveCacheKeyAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await _cacheService.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache {Key} REMOVE error: {Error}", key, ex.Message);
        }
    }

    /// <summary>
    ///     Synchronous counterpart to <see cref="RefreshCacheAsync"/>, used only by <see cref="SaveChanges"/>
    ///     so the sync <see cref="ICacheRepository{T}"/> surface never blocks a thread on async work
    ///     (no <c>GetAwaiter().GetResult()</c>) — it calls <see cref="ICacheService"/>'s own sync
    ///     members instead of awaiting the async ones.
    /// </summary>
    private void RefreshCache()
    {
        var key = CacheKey;

        try
        {
            _cacheService.Remove(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache {Key} REMOVE error: {Error}", key, ex.Message);
        }

        var data = _context.Set<T>()
            .AsNoTracking()
            .ToList();

        var lifeTime = TimeSpan.FromMinutes(35);

        _cacheService.TrySet(key, data, lifeTime);
    }
}
