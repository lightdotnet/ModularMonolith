using Light.Extensions.Caching;
using Microsoft.Extensions.Logging;
using StarterKit.Persistence.Repositories;

namespace Framework.Tests.Persistence.TestSupport;

/// <summary>
///     Minimal concrete <see cref="CacheRepositoryBase{T}"/> over <see cref="TestAggregate"/>/
///     <see cref="TestDbContext"/>, needed because <see cref="CacheRepositoryBase{T}"/> is abstract.
/// </summary>
/// <remarks>
///     The logger parameter type is <see cref="ILogger{TCategoryName}"/> of
///     <see cref="CacheRepositoryBase{T}"/> — not of this subclass — matching
///     <see cref="CacheRepositoryBase{T}"/>'s own constructor and the production
///     <c>CacheRepository&lt;TEntity, TContext&gt;</c>'s established pattern of logging under the
///     base type's category.
/// </remarks>
internal sealed class TestCacheRepository(
    TestDbContext context,
    ICacheService cacheService,
    ILogger<CacheRepositoryBase<TestAggregate>> logger)
    : CacheRepositoryBase<TestAggregate>(context, cacheService, logger);
