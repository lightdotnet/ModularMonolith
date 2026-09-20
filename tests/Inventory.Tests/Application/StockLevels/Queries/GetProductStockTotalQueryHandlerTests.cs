using Inventory.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using StarterKit.Inventory.Api.Application.StockLevels.Queries;
using StarterKit.Inventory.Api.Domain.StockLevels;
using Xunit;

namespace Inventory.Tests.Application.StockLevels.Queries;

public class GetProductStockTotalQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnZeroTotals_WhenProductHasNoStockLevelRows()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var handler = new GetProductStockTotalQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetProductStockTotalQuery(1),
            TestContext.Current.CancellationToken);

        // Assert — no rows is not a NotFound, just zero stock everywhere.
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.ProductId);
        Assert.Equal(0, result.Data.TotalQuantityOnHand);
        Assert.Equal(0, result.Data.LocationCount);
    }

    [Fact]
    public async Task Handle_ShouldSumQuantityAndCountLocations_WhenProductHasStockAtMultipleLocations()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var levelA = StockLevel.Create(1, "location-1");
        levelA.Apply(5);
        var levelB = StockLevel.Create(1, "location-2");
        levelB.Apply(3);
        host.Context.StockLevels.AddRange(levelA, levelB);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetProductStockTotalQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetProductStockTotalQuery(1),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(8, result.Data!.TotalQuantityOnHand);
        Assert.Equal(2, result.Data.LocationCount);
    }

    [Fact]
    public async Task Handle_ShouldExcludeZeroQuantityLocations_FromLocationCount()
    {
        // Arrange — a location whose running total was zeroed back out by offsetting movements still
        // has a StockLevel row (created lazily, never deleted); the handler's grouping filters it out
        // of LocationCount via `Count(x => x.QuantityOnHand > 0)`, even though it still contributes to
        // TotalQuantityOnHand as part of the Sum.
        using var host = new InventoryTestHost();
        var stockedLevel = StockLevel.Create(1, "location-1");
        stockedLevel.Apply(5);
        var zeroedLevel = StockLevel.Create(1, "location-2");
        zeroedLevel.Apply(4);
        zeroedLevel.Apply(-4);
        host.Context.StockLevels.AddRange(stockedLevel, zeroedLevel);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetProductStockTotalQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetProductStockTotalQuery(1),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Data!.TotalQuantityOnHand);
        Assert.Equal(1, result.Data.LocationCount);
    }
}
