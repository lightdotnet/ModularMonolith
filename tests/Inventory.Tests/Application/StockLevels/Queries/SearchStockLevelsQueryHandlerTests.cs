using Inventory.Tests.TestSupport;
using StarterKit.Inventory.Api.Application.StockLevels.Queries;
using StarterKit.Inventory.Api.Domain.StockLevels;
using StarterKit.Inventory.Contracts.Stock;
using Xunit;

namespace Inventory.Tests.Application.StockLevels.Queries;

public class SearchStockLevelsQueryHandlerTests
{
    private static async Task SeedAsync(InventoryTestHost host)
    {
        var level = StockLevel.Create(1, "location-1");
        level.Apply(3, 10m);
        host.Context.StockLevels.Add(level);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static SearchStockLevelRequest Request() =>
        new()
        {
            PageNumber = 1,
            PageSize = 20,
        };

    [Fact]
    public async Task Handle_ShouldNullTheCostFields_WhenIncludeCostIsFalse()
    {
        // Arrange
        using var host = new InventoryTestHost();
        await SeedAsync(host);
        var handler = new SearchStockLevelsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new SearchStockLevelsQuery(Request(), IncludeCost: false), TestContext.Current.CancellationToken);

        // Assert
        var dto = Assert.Single(result.Data.Records);
        Assert.Equal(3, dto.QuantityOnHand);
        Assert.Null(dto.AverageCostBase);
        Assert.Null(dto.TotalValueBase);
    }

    [Fact]
    public async Task Handle_ShouldNullTheCostFields_ByDefault()
    {
        using var host = new InventoryTestHost();
        await SeedAsync(host);
        var handler = new SearchStockLevelsQueryHandler(host.Context);

        var result = await handler.Handle(new SearchStockLevelsQuery(Request()), TestContext.Current.CancellationToken);

        var dto = Assert.Single(result.Data.Records);
        Assert.Null(dto.AverageCostBase);
        Assert.Null(dto.TotalValueBase);
    }

    [Fact]
    public async Task Handle_ShouldPopulateTheCostFields_WhenIncludeCostIsTrue()
    {
        // Arrange
        using var host = new InventoryTestHost();
        await SeedAsync(host);
        var handler = new SearchStockLevelsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new SearchStockLevelsQuery(Request(), IncludeCost: true), TestContext.Current.CancellationToken);

        // Assert
        var dto = Assert.Single(result.Data.Records);
        Assert.Equal(3.3333m, dto.AverageCostBase);
        Assert.Equal(10m, dto.TotalValueBase);
    }
}
