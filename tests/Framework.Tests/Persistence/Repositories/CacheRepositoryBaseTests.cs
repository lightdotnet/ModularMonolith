using Framework.Tests.Persistence.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StarterKit.Persistence.Repositories;
using Xunit;

namespace Framework.Tests.Persistence.Repositories;

/// <summary>
///     Exercises <see cref="CacheRepositoryBase{T}"/> against a real EF Core Sqlite in-memory
///     provider (not <see cref="TestDbContext.CreateInMemory"/>'s EF Core InMemory provider) plus
///     the hand-written <see cref="FakeCacheService"/>.
/// </summary>
/// <remarks>
///     Sqlite is required here, not the EF Core InMemory provider: <see cref="CacheRepositoryBase{T}.CacheKey"/>
///     calls <c>Database.GetDbConnection()</c>, which is relational-only and throws
///     <see cref="InvalidOperationException"/> against the InMemory provider ("Relational-specific
///     methods can only be used when the context is using a relational database provider."). Since
///     every <see cref="CacheRepositoryBase{T}"/> member reads <c>CacheKey</c>, this affects every
///     test in this file, not just the key-format one — hence following the same inline
///     "open Sqlite connection, <c>UseSqlite</c>, <c>EnsureCreatedAsync</c>" pattern already used by
///     <c>SqliteDbContextExtensionsTests</c> rather than the InMemory-provider pattern used by
///     <c>TrackingExtensionsTests</c>.
/// </remarks>
public class CacheRepositoryBaseTests
{
    [Fact]
    public async Task ToListAsync_ShouldQueryDatabaseAndPopulateCache_WhenCacheIsEmpty()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync(cancellationToken);
        context.Aggregates.AddRange(new TestAggregate(), new TestAggregate());
        await context.SaveChangesAsync(cancellationToken);
        var cacheService = new FakeCacheService();
        var repository = new TestCacheRepository(context, cacheService, NullLogger<CacheRepositoryBase<TestAggregate>>.Instance);

        // Act
        var result = await repository.ToListAsync(cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(cacheService.ContainsKey(repository.CacheKey));
    }

    [Fact]
    public async Task ToListAsync_ShouldReturnCachedData_WithoutQueryingDatabase_WhenCacheIsWarm()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync(cancellationToken);
        var cacheService = new FakeCacheService();
        var repository = new TestCacheRepository(context, cacheService, NullLogger<CacheRepositoryBase<TestAggregate>>.Instance);

        // Warm the cache with a known, empty snapshot before any rows exist in the DB, then add
        // rows directly to the DB afterward. A cache-first read must still return the empty
        // snapshot - proving it never fell through to RefreshCacheAsync/the DB query.
        IReadOnlyList<TestAggregate> cachedSnapshot = new List<TestAggregate>();
        cacheService.Seed(repository.CacheKey, cachedSnapshot);
        context.Aggregates.AddRange(new TestAggregate(), new TestAggregate());
        await context.SaveChangesAsync(cancellationToken);

        // Act
        var result = await repository.ToListAsync(cancellationToken);

        // Assert
        Assert.Empty(result);
        Assert.Same(cachedSnapshot, result);
    }

    [Fact]
    public async Task RefreshCacheAsync_ShouldQueryDatabaseAndOverwriteCache_RegardlessOfExistingCacheContent()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync(cancellationToken);
        var cacheService = new FakeCacheService();
        var repository = new TestCacheRepository(context, cacheService, NullLogger<CacheRepositoryBase<TestAggregate>>.Instance);

        // Seed the cache with stale content unrelated to the database.
        IReadOnlyList<TestAggregate> staleSnapshot = [new TestAggregate()];
        cacheService.Seed(repository.CacheKey, staleSnapshot);
        context.Aggregates.AddRange(new TestAggregate(), new TestAggregate());
        await context.SaveChangesAsync(cancellationToken);

        // Act
        var result = await repository.RefreshCacheAsync(cancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        var cached = await cacheService.TryGetAsync<IReadOnlyList<TestAggregate>>(repository.CacheKey, cancellationToken);
        Assert.Equal(2, cached?.Count);
    }

    [Fact]
    public async Task ToListAsync_ShouldReturnDetachedEntities_NotTrackedByTheDbContext()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync(cancellationToken);
        context.Aggregates.Add(new TestAggregate());
        await context.SaveChangesAsync(cancellationToken);
        var cacheService = new FakeCacheService();
        var repository = new TestCacheRepository(context, cacheService, NullLogger<CacheRepositoryBase<TestAggregate>>.Instance);

        // Act
        var result = await repository.ToListAsync(cancellationToken);

        // Assert
        var item = Assert.Single(result);
        Assert.Equal(EntityState.Detached, context.Entry(item).State);
        Assert.DoesNotContain(context.ChangeTracker.Entries<TestAggregate>(), e => ReferenceEquals(e.Entity, item));
    }

    [Fact]
    public void CacheKey_ShouldContainDatabaseNameAndEntityFullName()
    {
        // Arrange
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options);
        var cacheService = new FakeCacheService();
        var repository = new TestCacheRepository(context, cacheService, NullLogger<CacheRepositoryBase<TestAggregate>>.Instance);

        // Act
        var key = repository.CacheKey;

        // Assert
        Assert.Contains(typeof(TestAggregate).FullName!, key);
        Assert.Contains(connection.Database, key);
        Assert.Equal($"[{connection.Database}]_[{typeof(TestAggregate).FullName}]", key);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistChanges_AndRefreshCache()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync(cancellationToken);
        var cacheService = new FakeCacheService();
        var repository = new TestCacheRepository(context, cacheService, NullLogger<CacheRepositoryBase<TestAggregate>>.Instance);
        repository.Add(new TestAggregate());

        // Act
        var affected = await repository.SaveChangesAsync(cancellationToken);

        // Assert
        Assert.Equal(1, affected);
        Assert.Equal(1, await context.Aggregates.CountAsync(cancellationToken));
        var cached = await cacheService.TryGetAsync<IReadOnlyList<TestAggregate>>(repository.CacheKey, cancellationToken);
        Assert.Single(cached!);
    }

    [Fact]
    public async Task RemoveCacheAsync_ShouldRemoveTheCacheKey()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options);
        var cacheService = new FakeCacheService();
        var repository = new TestCacheRepository(context, cacheService, NullLogger<CacheRepositoryBase<TestAggregate>>.Instance);
        IReadOnlyList<TestAggregate> emptySnapshot = [];
        cacheService.Seed(repository.CacheKey, emptySnapshot);

        // Act
        await repository.RemoveCacheAsync(cancellationToken);

        // Assert
        Assert.False(cacheService.ContainsKey(repository.CacheKey));
    }

    [Fact]
    public async Task RefreshCacheAsync_ShouldSwallowCacheRemoveFailure_AndStillReturnFreshData()
    {
        // Arrange: the private TryRemoveCacheKeyAsync swallow path - a cache-backend hiccup must
        // not surface as a failure of an already-committed DB write.
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync(cancellationToken);
        context.Aggregates.Add(new TestAggregate());
        await context.SaveChangesAsync(cancellationToken);
        var cacheService = new FakeCacheService { ThrowOnRemove = true };
        var repository = new TestCacheRepository(context, cacheService, NullLogger<CacheRepositoryBase<TestAggregate>>.Instance);

        // Act
        var exception = await Record.ExceptionAsync(() => repository.RefreshCacheAsync(cancellationToken));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task RemoveCacheAsync_ShouldLetCacheRemoveFailurePropagate()
    {
        // Arrange: unlike RefreshCacheAsync's internal swallow path, an explicit invalidation
        // request failing is expected to be visible to the caller.
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options);
        var cacheService = new FakeCacheService { ThrowOnRemove = true };
        var repository = new TestCacheRepository(context, cacheService, NullLogger<CacheRepositoryBase<TestAggregate>>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.RemoveCacheAsync(cancellationToken));
    }
}
