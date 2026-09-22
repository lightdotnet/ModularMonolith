using Inventory.Tests.TestSupport;
using StarterKit.Inventory.Api.Application.StockAdjustments.Queries;
using StarterKit.Inventory.Api.Domain.StockAdjustments;
using StarterKit.Inventory.Contracts.Stock;
using Xunit;

namespace Inventory.Tests.Application.StockAdjustments.Queries;

public class SearchStockAdjustmentsCostVisibilityTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static async Task SeedAsync(InventoryTestHost host)
    {
        host.Context.StockAdjustments.Add(StockAdjustment.Create(
            1,
            "location-1",
            4,
            StockAdjustmentReason.ManualAdjustment,
            Now,
            "user-1",
            unitCostBase: 2.5m,
            valueDeltaBase: 10m));
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static SearchStockAdjustmentRequest Request() =>
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
        var handler = new SearchStockAdjustmentsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new SearchStockAdjustmentsQuery(Request(), IncludeCost: false), TestContext.Current.CancellationToken);

        // Assert
        var dto = Assert.Single(result.Data.Records);
        Assert.Equal(4, dto.QuantityDelta);
        Assert.Null(dto.UnitCostBase);
        Assert.Null(dto.ValueDeltaBase);
    }

    [Fact]
    public async Task Handle_ShouldNullTheCostFields_ByDefault()
    {
        using var host = new InventoryTestHost();
        await SeedAsync(host);
        var handler = new SearchStockAdjustmentsQueryHandler(host.Context);

        var result = await handler.Handle(new SearchStockAdjustmentsQuery(Request()), TestContext.Current.CancellationToken);

        var dto = Assert.Single(result.Data.Records);
        Assert.Null(dto.UnitCostBase);
        Assert.Null(dto.ValueDeltaBase);
    }

    [Fact]
    public async Task Handle_ShouldPopulateTheCostFields_WhenIncludeCostIsTrue()
    {
        // Arrange
        using var host = new InventoryTestHost();
        await SeedAsync(host);
        var handler = new SearchStockAdjustmentsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new SearchStockAdjustmentsQuery(Request(), IncludeCost: true), TestContext.Current.CancellationToken);

        // Assert
        var dto = Assert.Single(result.Data.Records);
        Assert.Equal(2.5m, dto.UnitCostBase);
        Assert.Equal(10m, dto.ValueDeltaBase);
    }
}
