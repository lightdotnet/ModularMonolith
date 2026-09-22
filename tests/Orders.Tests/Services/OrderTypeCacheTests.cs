using Light.Extensions.Caching;
using Moq;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Domain.OrderTypes;
using StarterKit.Orders.Api.Services;
using StarterKit.Orders.Contracts.Common;
using Xunit;

namespace Orders.Tests.Services;

/// <summary>
/// Exercises <see cref="OrderTypeCache"/> against a mocked <see cref="ICacheService"/> (stateful,
/// backed by a local field standing in for whatever the real distributed/memory cache implementation
/// would store) plus a real Sqlite-backed <see cref="OrdersTestHost"/> for the "reload from DB" side.
/// Mirrors <c>Location.Tests.Services.LocationTypeCacheTests</c>. Replaces the former separate
/// <c>FeeTypeCacheTests</c>/<c>PaymentTypeCacheTests</c>.
/// </summary>
public class OrderTypeCacheTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReloadFromDatabase_WhenCacheIsEmpty()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await SeedTypeAsync(host, "SHIPPING", OrderTypeCategory.Fee);
        await SeedTypeAsync(host, "CASH", OrderTypeCategory.Payment);
        var (cacheMock, _) = CreateStatefulCacheMock();
        var cache = new OrderTypeCache(cacheMock.Object, host.Context);

        // Act
        var result = await cache.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Id == "SHIPPING" && x.Category == OrderTypeCategory.Fee);
        Assert.Contains(result, x => x.Id == "CASH" && x.Category == OrderTypeCategory.Payment);
        cacheMock.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<List<OrderType>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReloadAsync_ShouldWriteFullCurrentListToCache()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await SeedTypeAsync(host, "SHIPPING", OrderTypeCategory.Fee);
        await SeedTypeAsync(host, "CASH", OrderTypeCategory.Payment);
        var (cacheMock, currentValue) = CreateStatefulCacheMock();
        var cache = new OrderTypeCache(cacheMock.Object, host.Context);

        // Act
        await cache.ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, currentValue()!.Count);
        cacheMock.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<List<OrderType>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnMatchingEntry_WithoutRequeryingDatabase_WhenCachePopulated()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await SeedTypeAsync(host, "SHIPPING", OrderTypeCategory.Fee);
        await SeedTypeAsync(host, "CASH", OrderTypeCategory.Payment);
        var (cacheMock, _) = CreateStatefulCacheMock();
        var cache = new OrderTypeCache(cacheMock.Object, host.Context);
        await cache.ReloadAsync(TestContext.Current.CancellationToken);

        // Remove the underlying rows so a real DB hit (as opposed to a cache hit) would come back empty.
        host.Context.OrderTypes.RemoveRange(host.Context.OrderTypes);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await cache.GetAsync("SHIPPING", OrderTypeCategory.Fee, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SHIPPING", result!.Id);
        cacheMock.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<List<OrderType>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenIdNotFound()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await SeedTypeAsync(host, "SHIPPING", OrderTypeCategory.Fee);
        var (cacheMock, _) = CreateStatefulCacheMock();
        var cache = new OrderTypeCache(cacheMock.Object, host.Context);
        await cache.ReloadAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await cache.GetAsync("MISSING", OrderTypeCategory.Fee, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Proves the lookup is keyed by the full composite <c>(Id, Category)</c> pair — a matching Id
    /// cached under a different category must not be returned.
    /// </summary>
    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenIdExistsOnlyUnderADifferentCategory()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await SeedTypeAsync(host, "OTHER", OrderTypeCategory.Fee);
        var (cacheMock, _) = CreateStatefulCacheMock();
        var cache = new OrderTypeCache(cacheMock.Object, host.Context);
        await cache.ReloadAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await cache.GetAsync("OTHER", OrderTypeCategory.Payment, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    private static (Mock<ICacheService> Mock, Func<List<OrderType>?> CurrentValue) CreateStatefulCacheMock()
    {
        List<OrderType>? stored = null;

        var mock = new Mock<ICacheService>();

        mock.Setup(x => x.GetAsync<List<OrderType>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => stored!);

        mock.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<List<OrderType>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, List<OrderType>, TimeSpan?, CancellationToken>((_, value, _, _) => stored = value)
            .Returns(Task.CompletedTask);

        return (mock, () => stored);
    }

    private static async Task<OrderType> SeedTypeAsync(OrdersTestHost host, string id, OrderTypeCategory category)
    {
        var type = OrderType.Create(id, category, id);
        await host.Context.OrderTypes.AddAsync(type, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return type;
    }
}
