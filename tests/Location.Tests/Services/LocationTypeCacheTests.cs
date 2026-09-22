using Light.Extensions.Caching;
using Location.Tests.TestSupport;
using Moq;
using StarterKit.Locations.Api.Domain.LocationTypes;
using StarterKit.Locations.Api.Services;
using Xunit;

namespace Location.Tests.Services;

/// <summary>
/// Exercises <see cref="LocationTypeCache"/> against a mocked <see cref="ICacheService"/> (stateful,
/// backed by a local field standing in for whatever the real distributed/memory cache implementation
/// would store) plus a real Sqlite-backed <see cref="LocationTestHost"/> for the "reload from DB"
/// side.
/// </summary>
public class LocationTypeCacheTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReloadFromDatabase_WhenCacheIsEmpty()
    {
        // Arrange
        using var host = new LocationTestHost();
        await SeedTypeAsync(host, "STORE");
        await SeedTypeAsync(host, "WAREHOUSE");
        var (cacheMock, _) = CreateStatefulCacheMock();
        var cache = new LocationTypeCache(cacheMock.Object, host.Context);

        // Act
        var result = await cache.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Id == "STORE");
        Assert.Contains(result, x => x.Id == "WAREHOUSE");
        cacheMock.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<List<LocationType>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReloadAsync_ShouldWriteFullCurrentListToCache()
    {
        // Arrange
        using var host = new LocationTestHost();
        await SeedTypeAsync(host, "STORE");
        await SeedTypeAsync(host, "WAREHOUSE");
        await SeedTypeAsync(host, "BIN", allowedParentTypeId: "STORE");
        var (cacheMock, currentValue) = CreateStatefulCacheMock();
        var cache = new LocationTypeCache(cacheMock.Object, host.Context);

        // Act
        await cache.ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, currentValue()!.Count);
        cacheMock.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<List<LocationType>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnMatchingEntry_WithoutRequeryingDatabase_WhenCachePopulated()
    {
        // Arrange
        using var host = new LocationTestHost();
        await SeedTypeAsync(host, "STORE");
        await SeedTypeAsync(host, "WAREHOUSE");
        var (cacheMock, _) = CreateStatefulCacheMock();
        var cache = new LocationTypeCache(cacheMock.Object, host.Context);
        await cache.ReloadAsync(TestContext.Current.CancellationToken);

        // Remove the underlying rows so a real DB hit (as opposed to a cache hit) would come back empty.
        host.Context.LocationTypes.RemoveRange(host.Context.LocationTypes);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await cache.GetAsync("STORE", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("STORE", result!.Id);
        cacheMock.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<List<LocationType>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenIdNotFound()
    {
        // Arrange
        using var host = new LocationTestHost();
        await SeedTypeAsync(host, "STORE");
        var (cacheMock, _) = CreateStatefulCacheMock();
        var cache = new LocationTypeCache(cacheMock.Object, host.Context);
        await cache.ReloadAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await cache.GetAsync("MISSING", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Builds an <see cref="ICacheService"/> mock backed by a simple local field, so
    /// <c>SetAsync</c>/<c>GetAsync</c> round-trip like a real cache instead of each being an isolated
    /// stub — needed to exercise <see cref="LocationTypeCache"/>'s "reload, then read back" flow.
    /// </summary>
    private static (Mock<ICacheService> Mock, Func<List<LocationType>?> CurrentValue) CreateStatefulCacheMock()
    {
        List<LocationType>? stored = null;

        var mock = new Mock<ICacheService>();

        mock.Setup(x => x.GetAsync<List<LocationType>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => stored!);

        mock.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<List<LocationType>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, List<LocationType>, TimeSpan?, CancellationToken>((_, value, _, _) => stored = value)
            .Returns(Task.CompletedTask);

        return (mock, () => stored);
    }

    private static async Task<LocationType> SeedTypeAsync(
        LocationTestHost host,
        string id,
        string? allowedParentTypeId = null,
        bool canHaveChildren = true)
    {
        var type = LocationType.Create(id, id, allowedParentTypeId, canHaveChildren);
        await host.Context.LocationTypes.AddAsync(type, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return type;
    }
}
