using Inventory.Tests.TestSupport;
using StarterKit.Inventory.Api.Application.StockLevels.Queries;
using StarterKit.Inventory.Api.Domain.StockLevels;
using StarterKit.Inventory.Contracts.Stock;
using Xunit;

namespace Inventory.Tests.Application.StockLevels.Queries;

public class SearchStockValuationQueryHandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task SeedAsync(
        InventoryTestHost host,
        params (long ProductId, string LocationId, int Quantity, decimal Value)[] levels)
    {
        foreach (var (productId, locationId, quantity, value) in levels)
        {
            var level = StockLevel.Create(productId, locationId);

            if (quantity != 0)
                level.Apply(quantity, value);

            host.Context.StockLevels.Add(level);
        }

        await host.Context.SaveChangesAsync(Ct);
    }

    private static SearchStockValuationQuery Query(
        long? productId = null,
        string? locationId = null,
        int pageNumber = 1,
        int pageSize = 20) =>
        new(new SearchStockValuationRequest
        {
            ProductId = productId,
            LocationId = locationId,
            PageNumber = pageNumber,
            PageSize = pageSize,
        });

    [Fact]
    public async Task Handle_ShouldReturnOneLinePerLevel_WithAverageAndValue_OrderedByProductThenLocation()
    {
        // Arrange
        using var host = new InventoryTestHost();
        await SeedAsync(host, (2, "loc-a", 4, 10m), (1, "loc-b", 3, 10m), (1, "loc-a", 2, 5m));
        var handler = new SearchStockValuationQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(Query(), Ct);

        // Assert
        Assert.True(result.IsSuccess);
        var lines = result.Data!.Lines;
        Assert.Equal(new[] { (1L, "loc-a"), (1L, "loc-b"), (2L, "loc-a") }, lines.Select(x => (x.ProductId, x.LocationId)));

        var second = lines[1];
        Assert.Equal(3, second.QuantityOnHand);
        Assert.Equal(10m, second.TotalValueBase);
        Assert.Equal(3.3333m, second.AverageCostBase);
    }

    [Fact]
    public async Task Handle_ShouldOnlyIncludeLevelsWithStockOnHand()
    {
        // Arrange — the emptied level must not appear in lines, count or totals.
        using var host = new InventoryTestHost();
        await SeedAsync(host, (1, "loc-a", 2, 5m), (2, "loc-a", 0, 0m));
        var handler = new SearchStockValuationQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(Query(), Ct);

        // Assert
        var data = result.Data!;
        Assert.Equal(1, data.TotalRecords);
        Assert.Equal(1L, Assert.Single(data.Lines).ProductId);
        Assert.Equal(2, data.GrandTotalQuantity);
        Assert.Equal(5m, data.GrandTotalValueBase);
    }

    [Fact]
    public async Task Handle_ShouldPageTheLines_ButTotalEveryMatchingLevel()
    {
        // Arrange
        using var host = new InventoryTestHost();
        await SeedAsync(
            host,
            (1, "loc-a", 1, 1.5m),
            (2, "loc-a", 2, 2.25m),
            (3, "loc-a", 3, 3.125m),
            (4, "loc-a", 4, 4.0625m),
            (5, "loc-a", 5, 5m));
        var handler = new SearchStockValuationQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(Query(pageNumber: 2, pageSize: 2), Ct);

        // Assert
        var data = result.Data!;
        Assert.Equal(2, data.PageNumber);
        Assert.Equal(2, data.PageSize);
        Assert.Equal(5, data.TotalRecords);
        Assert.Equal(new[] { 3L, 4L }, data.Lines.Select(x => x.ProductId));
        Assert.Equal(15, data.GrandTotalQuantity);
        Assert.Equal(15.9375m, data.GrandTotalValueBase);
    }

    [Fact]
    public async Task Handle_ShouldReturnZeroTotals_WhenNothingMatches()
    {
        using var host = new InventoryTestHost();
        var handler = new SearchStockValuationQueryHandler(host.Context);

        var result = await handler.Handle(Query(), Ct);

        var data = result.Data!;
        Assert.Empty(data.Lines);
        Assert.Equal(0, data.TotalRecords);
        Assert.Equal(0, data.GrandTotalQuantity);
        Assert.Equal(0m, data.GrandTotalValueBase);
    }

    [Fact]
    public async Task Handle_ShouldFilterByProduct_AndLimitTheTotalsToTheFilter()
    {
        // Arrange
        using var host = new InventoryTestHost();
        await SeedAsync(host, (1, "loc-a", 2, 5m), (1, "loc-b", 3, 6m), (2, "loc-a", 7, 100m));
        var handler = new SearchStockValuationQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(Query(productId: 1), Ct);

        // Assert
        var data = result.Data!;
        Assert.Equal(2, data.TotalRecords);
        Assert.Equal(5, data.GrandTotalQuantity);
        Assert.Equal(11m, data.GrandTotalValueBase);
    }

    [Fact]
    public async Task Handle_ShouldFilterByLocation_AndLimitTheTotalsToTheFilter()
    {
        // Arrange
        using var host = new InventoryTestHost();
        await SeedAsync(host, (1, "loc-a", 2, 5m), (1, "loc-b", 3, 6m), (2, "loc-a", 7, 100m));
        var handler = new SearchStockValuationQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(Query(locationId: "loc-a"), Ct);

        // Assert
        var data = result.Data!;
        Assert.Equal(2, data.TotalRecords);
        Assert.Equal(9, data.GrandTotalQuantity);
        Assert.Equal(105m, data.GrandTotalValueBase);
        Assert.All(data.Lines, x => Assert.Equal("loc-a", x.LocationId));
    }

    [Fact]
    public async Task Handle_ShouldSumQuantityAsALong_WhenTheTotalExceedsAnInt()
    {
        // Arrange
        using var host = new InventoryTestHost();
        await SeedAsync(host, (1, "loc-a", int.MaxValue, 1m), (2, "loc-a", int.MaxValue, 1m));
        var handler = new SearchStockValuationQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(Query(), Ct);

        // Assert
        Assert.Equal(2L * int.MaxValue, result.Data!.GrandTotalQuantity);
    }
}
