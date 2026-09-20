using Inventory.Tests.TestSupport;
using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;
using StarterKit.Inventory.Api.Domain.StockAdjustments;
using StarterKit.Inventory.Api.Services;
using StarterKit.Inventory.Contracts.Stock;
using StarterKit.Locations.Contracts.Services;
using Xunit;

namespace Inventory.Tests.Services;

/// <summary>
/// Exercises <see cref="InventoryService"/> end-to-end against a real (Sqlite) <see cref="StockLedger"/>
/// and <see cref="StarterKit.Inventory.Api.Data.InventoryDbContext"/> — only the cross-module
/// <see cref="ILocationDirectoryService"/> seam is mocked. Mirrors
/// <c>Orders.Tests.Application.Orders.Commands.CreateOrderCommandHandlerTests</c>'s
/// real-context-plus-mocked-cross-module-service shape.
/// </summary>
public class InventoryServiceTests
{
    private const string LocationId = "location-1";

    private static Mock<ILocationDirectoryService> MakeLocationServiceMock(bool locationExists = true)
    {
        var mock = new Mock<ILocationDirectoryService>();
        mock.Setup(s => s.ExistsAsync(LocationId, It.IsAny<CancellationToken>())).ReturnsAsync(locationExists);
        return mock;
    }

    private static async Task SeedStockAsync(
        InventoryTestHost host,
        StockLedger ledger,
        long productId,
        int quantity)
    {
        await ledger.ApplyAsync(
            [new StockMovement(productId, LocationId, quantity, StockAdjustmentReason.ManualAdjustment, "seed-user")],
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DecrementForOrderAsync_ShouldDecrementEveryLine_WhenStockIsSufficient()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        await SeedStockAsync(host, ledger, productId: 1, quantity: 10);
        await SeedStockAsync(host, ledger, productId: 2, quantity: 5);
        var service = new InventoryService(ledger, MakeLocationServiceMock().Object);

        // Act
        await service.DecrementForOrderAsync(
            orderId: 100,
            LocationId,
            [new StockLine(1, 10, 3), new StockLine(2, 20, 2)],
            "user-1",
            TestContext.Current.CancellationToken);

        // Assert
        var level1 = await host.Context.StockLevels.FirstAsync(x => x.ProductId == 1, TestContext.Current.CancellationToken);
        var level2 = await host.Context.StockLevels.FirstAsync(x => x.ProductId == 2, TestContext.Current.CancellationToken);
        Assert.Equal(7, level1.QuantityOnHand);
        Assert.Equal(3, level2.QuantityOnHand);

        var placements = await host.Context.StockAdjustments
            .Where(x => x.SourceOrderId == 100 && x.Reason == StockAdjustmentReason.OrderPlacement)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, placements.Count);
        Assert.Contains(placements, x => x.ProductId == 1 && x.QuantityDelta == -3 && x.IdempotencyKey == "order-place:100:10");
        Assert.Contains(placements, x => x.ProductId == 2 && x.QuantityDelta == -2 && x.IdempotencyKey == "order-place:100:20");
    }

    [Fact]
    public async Task DecrementForOrderAsync_ShouldApplyNothing_AndListEveryShortfall_WhenAnyLineWouldOversell()
    {
        // Arrange — two lines oversell, one has plenty: the batch must be all-or-nothing.
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        await SeedStockAsync(host, ledger, productId: 1, quantity: 1);
        await SeedStockAsync(host, ledger, productId: 2, quantity: 1);
        await SeedStockAsync(host, ledger, productId: 3, quantity: 10);
        var service = new InventoryService(ledger, MakeLocationServiceMock().Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.DecrementForOrderAsync(
            orderId: 200,
            LocationId,
            [new StockLine(1, 10, 5), new StockLine(2, 20, 5), new StockLine(3, 30, 2)],
            "user-1",
            TestContext.Current.CancellationToken));

        Assert.Contains("1 at location-1", exception.Message);
        Assert.Contains("2 at location-1", exception.Message);
        Assert.DoesNotContain("3 at location-1", exception.Message);

        var level1 = await host.Context.StockLevels.FirstAsync(x => x.ProductId == 1, TestContext.Current.CancellationToken);
        var level2 = await host.Context.StockLevels.FirstAsync(x => x.ProductId == 2, TestContext.Current.CancellationToken);
        var level3 = await host.Context.StockLevels.FirstAsync(x => x.ProductId == 3, TestContext.Current.CancellationToken);
        Assert.Equal(1, level1.QuantityOnHand);
        Assert.Equal(1, level2.QuantityOnHand);
        Assert.Equal(10, level3.QuantityOnHand);

        Assert.False(await host.Context.StockAdjustments.AnyAsync(
            x => x.SourceOrderId == 200, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DecrementForOrderAsync_ShouldBeANoOp_WhenReplayedWithTheSameOrderAndLines()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        await SeedStockAsync(host, ledger, productId: 1, quantity: 10);
        var service = new InventoryService(ledger, MakeLocationServiceMock().Object);
        var lines = new List<StockLine> { new(1, 10, 3) };

        // Act
        await service.DecrementForOrderAsync(300, LocationId, lines, "user-1", TestContext.Current.CancellationToken);
        await service.DecrementForOrderAsync(300, LocationId, lines, "user-1", TestContext.Current.CancellationToken);

        // Assert
        var level = await host.Context.StockLevels.FirstAsync(x => x.ProductId == 1, TestContext.Current.CancellationToken);
        Assert.Equal(7, level.QuantityOnHand);
        var placements = await host.Context.StockAdjustments
            .Where(x => x.SourceOrderId == 300 && x.Reason == StockAdjustmentReason.OrderPlacement)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(placements);
    }

    [Fact]
    public async Task DecrementForOrderAsync_ShouldThrowValidationException_WhenLocationDoesNotExist()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        var service = new InventoryService(ledger, MakeLocationServiceMock(locationExists: false).Object);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.DecrementForOrderAsync(
            400, LocationId, [new StockLine(1, 10, 1)], "user-1", TestContext.Current.CancellationToken));

        Assert.False(await host.Context.StockAdjustments.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RestoreForOrderAsync_ShouldReverseEveryPlacementAdjustment_AndBeIdempotent()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        await SeedStockAsync(host, ledger, productId: 1, quantity: 10);
        var service = new InventoryService(ledger, MakeLocationServiceMock().Object);
        await service.DecrementForOrderAsync(
            500, LocationId, [new StockLine(1, 10, 3)], "user-1", TestContext.Current.CancellationToken);

        // Act — first restore reverses the placement.
        await service.RestoreForOrderAsync(500, "user-2", TestContext.Current.CancellationToken);

        // Assert
        var level = await host.Context.StockLevels.FirstAsync(x => x.ProductId == 1, TestContext.Current.CancellationToken);
        Assert.Equal(10, level.QuantityOnHand);
        var reversal = await host.Context.StockAdjustments.SingleAsync(
            x => x.Reason == StockAdjustmentReason.OrderCancellationRestore && x.SourceOrderId == 500,
            TestContext.Current.CancellationToken);
        Assert.Equal(3, reversal.QuantityDelta);
        Assert.Equal("user-2", reversal.PerformedByUserId);

        var original = await host.Context.StockAdjustments.SingleAsync(
            x => x.Reason == StockAdjustmentReason.OrderPlacement && x.SourceOrderId == 500,
            TestContext.Current.CancellationToken);
        Assert.Equal(original.Id, reversal.ReversesAdjustmentId);
        Assert.Null(original.IdempotencyKey);

        // Act — replaying the restore must not create a second reversal or change stock again.
        await service.RestoreForOrderAsync(500, "user-2", TestContext.Current.CancellationToken);

        var levelAfterReplay = await host.Context.StockLevels.FirstAsync(x => x.ProductId == 1, TestContext.Current.CancellationToken);
        Assert.Equal(10, levelAfterReplay.QuantityOnHand);
        var reversalCount = await host.Context.StockAdjustments.CountAsync(
            x => x.Reason == StockAdjustmentReason.OrderCancellationRestore && x.SourceOrderId == 500,
            TestContext.Current.CancellationToken);
        Assert.Equal(1, reversalCount);
    }

    [Fact]
    public async Task RestoreForOrderAsync_ShouldReleaseTheOriginalIdempotencyKey_SoTheSameOrderCanBeDecrementedAgain()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        await SeedStockAsync(host, ledger, productId: 1, quantity: 10);
        var service = new InventoryService(ledger, MakeLocationServiceMock().Object);
        var lines = new List<StockLine> { new(1, 10, 3) };
        await service.DecrementForOrderAsync(600, LocationId, lines, "user-1", TestContext.Current.CancellationToken);
        await service.RestoreForOrderAsync(600, "user-1", TestContext.Current.CancellationToken);

        // Act — the key freed by the restore must let the same order/line decrement again.
        await service.DecrementForOrderAsync(600, LocationId, lines, "user-1", TestContext.Current.CancellationToken);

        // Assert
        var level = await host.Context.StockLevels.FirstAsync(x => x.ProductId == 1, TestContext.Current.CancellationToken);
        Assert.Equal(7, level.QuantityOnHand);
        var placements = await host.Context.StockAdjustments
            .Where(x => x.SourceOrderId == 600 && x.Reason == StockAdjustmentReason.OrderPlacement)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, placements.Count);
    }
}
