namespace StarterKit.Persistence.Repositories;

/// <summary>
///     Can be used to auto load list of T on cache,
///         query, add, update, remove instances of T.
/// </summary>
/// <remarks>
///     <para>
///         Scope: opt-in only, for small, infrequently-written reference/lookup tables that
///         benefit from a whole-table in-memory cache (e.g. a type catalog). This is NOT a
///         general replacement for this solution's default direct-DbContext-plus-Specification
///         access pattern used by every module — most entities should keep using that pattern.
///     </para>
///     <para>
///         <b>Write-path contract (unenforced):</b> any entity type cached through this repository
///         MUST be written exclusively through this repository — never call
///         <c>SaveChanges</c>/<c>SaveChangesAsync</c> directly on the underlying <see cref="DbContext"/>
///         for that entity type, or the cache will silently go stale with no error. This is a real,
///         unenforced constraint — audit call sites by hand if you adopt this for a new entity.
///     </para>
/// </remarks>
public interface ICacheRepository<T>
    : Light.Repositories.IRepository<T>, Light.Repositories.ISaveChanges
    where T : class
{
    /// <summary>
    ///     Reload list of T to cache.
    /// </summary>
    Task<IReadOnlyList<T>> RefreshCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete list of T on cache.
    /// </summary>
    Task RemoveCacheAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Cache repository with multi DbContext
/// </summary>
public interface ICacheRepository<TEntity, TContext> : ICacheRepository<TEntity>
    where TEntity : class
    where TContext : DbContext;