using Inventory.Tests.TestSupport;
using StarterKit.Inventory.Api.Application.StockAdjustments.Queries;
using StarterKit.Inventory.Api.Domain.StockAdjustments;
using StarterKit.Inventory.Contracts.Common;
using StarterKit.Inventory.Contracts.Stock;
using Xunit;

namespace Inventory.Tests.Application.StockAdjustments.Queries;

public class SearchStockAdjustmentsQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static async Task SeedAsync(
        InventoryTestHost host,
        params StockAdjustment[] adjustments)
    {
        host.Context.StockAdjustments.AddRange(adjustments);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static StockAdjustment ManualAdjustment() =>
        StockAdjustment.Create(
            1,
            "location-1",
            5,
            StockAdjustmentReason.ManualAdjustment,
            Now,
            "user-1");

    private static StockAdjustment OrderPlacement(
        long orderId,
        long lineId) =>
        StockAdjustment.Create(
            1,
            "location-1",
            -1,
            StockAdjustmentReason.OrderPlacement,
            Now.AddMinutes(1),
            "user-1",
            sourceType: StockSourceType.Order,
            sourceId: orderId,
            sourceLineId: lineId,
            idempotencyKey: $"order-place:{orderId}:{lineId}");

    [Fact]
    public async Task Handle_ShouldFilterBySourceOrderId_MatchingOnlyOrderSourcedAdjustments()
    {
        // Arrange
        using var host = new InventoryTestHost();
        await SeedAsync(host, ManualAdjustment(), OrderPlacement(100, 1), OrderPlacement(200, 1));
        var handler = new SearchStockAdjustmentsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchStockAdjustmentsQuery(new SearchStockAdjustmentRequest
            {
                SourceOrderId = 100,
                PageNumber = 1,
                PageSize = 20,
            }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        var dto = Assert.Single(result.Data.Records);
        Assert.Equal(100, dto.SourceOrderId);
        Assert.Equal(1, dto.SourceOrderLineId);
    }

    [Fact]
    public async Task Handle_ShouldLeaveSourceOrderIdNull_ForManualAndNonOrderSourcedAdjustments()
    {
        // Arrange — a non-order source that happens to reuse a numeric id must not leak as an order id.
        using var host = new InventoryTestHost();
        var transfer = StockAdjustment.Create(
            1,
            "location-1",
            3,
            StockAdjustmentReason.ManualAdjustment,
            Now.AddMinutes(2),
            "user-1",
            sourceType: StockSourceType.Transfer,
            sourceId: 100,
            sourceLineId: 4);
        await SeedAsync(host, ManualAdjustment(), transfer);
        var handler = new SearchStockAdjustmentsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchStockAdjustmentsQuery(new SearchStockAdjustmentRequest { PageNumber = 1, PageSize = 20 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Data.TotalRecords);
        Assert.All(result.Data.Records, x => Assert.Null(x.SourceOrderId));
        Assert.All(result.Data.Records, x => Assert.Null(x.SourceOrderLineId));
    }

    [Fact]
    public async Task Handle_ShouldNotMatchANonOrderSource_WhenFilteringBySourceOrderId()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var transfer = StockAdjustment.Create(
            1,
            "location-1",
            3,
            StockAdjustmentReason.ManualAdjustment,
            Now,
            "user-1",
            sourceType: StockSourceType.Transfer,
            sourceId: 100);
        await SeedAsync(host, transfer);
        var handler = new SearchStockAdjustmentsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchStockAdjustmentsQuery(new SearchStockAdjustmentRequest
            {
                SourceOrderId = 100,
                PageNumber = 1,
                PageSize = 20,
            }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, result.Data.TotalRecords);
    }
}
