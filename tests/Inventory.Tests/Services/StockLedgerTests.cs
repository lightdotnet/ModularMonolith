using Inventory.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using StarterKit.Inventory.Api.Domain.StockAdjustments;
using StarterKit.Inventory.Api.Services;
using StarterKit.Inventory.Contracts.Common;
using Xunit;

namespace Inventory.Tests.Services;

/// <summary>
/// Covers the reconciliation-facing members of <see cref="StockLedger"/> against a real Sqlite
/// <see cref="StarterKit.Inventory.Api.Data.InventoryDbContext"/>. Timestamps are whole minutes apart
/// because the Sqlite test provider stores <c>DateTimeOffset</c> with one-second precision.
/// </summary>
public class StockLedgerTests
{
    private const string LocationId = "location-1";

    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static async Task SeedStockAsync(
        StockLedger ledger,
        long productId,
        int quantity)
    {
        await ledger.ApplyAsync(
            [new StockMovement(productId, LocationId, quantity, StockAdjustmentReason.ManualAdjustment, "seed-user", UnitCostBase: 1m)],
            TestContext.Current.CancellationToken);
    }

    // Posts an order-placement decrement stamped with the host clock's current time.
    private static async Task PlaceAsync(
        StockLedger ledger,
        long orderId,
        long productId = 1,
        int quantity = 1,
        long lineId = 1)
    {
        await ledger.ApplyAsync(
            [
                new StockMovement(
                    productId,
                    LocationId,
                    -quantity,
                    StockAdjustmentReason.OrderPlacement,
                    "user-1",
                    SourceType: StockSourceType.Order,
                    SourceId: orderId,
                    SourceLineId: lineId,
                    IdempotencyKey: $"order-place:{orderId}:{lineId}")
            ],
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FilterSourceIdsWithUnreversedPostingsAsync_ShouldReturnOrder_WhenItHasAnUnreversedPlacement()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        host.DateTime.UtcNow = T0;
        await SeedStockAsync(ledger, 1, 10);
        await PlaceAsync(ledger, orderId: 100);

        // Act
        var result = await ledger.FilterSourceIdsWithUnreversedPostingsAsync(
            StockSourceType.Order,
            [100],
            T0.AddMinutes(5),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new long[] { 100 }, result);
    }

    [Fact]
    public async Task FilterSourceIdsWithUnreversedPostingsAsync_ShouldExcludeOrder_WhenItsPlacementWasReversed()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        host.DateTime.UtcNow = T0;
        await SeedStockAsync(ledger, 1, 10);
        await PlaceAsync(ledger, orderId: 100);
        await PlaceAsync(ledger, orderId: 200);
        await ledger.ReverseOrderPlacementsAsync(100, "user-2", TestContext.Current.CancellationToken);

        // Act
        var result = await ledger.FilterSourceIdsWithUnreversedPostingsAsync(
            StockSourceType.Order,
            [100, 200],
            T0.AddMinutes(5),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new long[] { 200 }, result);
    }

    [Fact]
    public async Task FilterSourceIdsWithUnreversedPostingsAsync_ShouldExcludePlacements_NewerThanPostedBefore()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        host.DateTime.UtcNow = T0;
        await SeedStockAsync(ledger, 1, 10);
        await PlaceAsync(ledger, orderId: 100);
        host.DateTime.UtcNow = T0.AddMinutes(10);
        await PlaceAsync(ledger, orderId: 200);

        // Act
        var result = await ledger.FilterSourceIdsWithUnreversedPostingsAsync(
            StockSourceType.Order,
            [100, 200],
            T0.AddMinutes(5),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new long[] { 100 }, result);
    }

    [Fact]
    public async Task FilterSourceIdsWithUnreversedPostingsAsync_ShouldIncludePlacement_PostedExactlyAtPostedBefore()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        host.DateTime.UtcNow = T0;
        await SeedStockAsync(ledger, 1, 10);
        await PlaceAsync(ledger, orderId: 100);

        // Act
        var result = await ledger.FilterSourceIdsWithUnreversedPostingsAsync(
            StockSourceType.Order,
            [100],
            T0,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new long[] { 100 }, result);
    }

    [Fact]
    public async Task FilterSourceIdsWithUnreversedPostingsAsync_ShouldIgnoreIds_NotInTheCandidateSet()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        host.DateTime.UtcNow = T0;
        await SeedStockAsync(ledger, 1, 10);
        await PlaceAsync(ledger, orderId: 100);
        await PlaceAsync(ledger, orderId: 200);

        // Act
        var result = await ledger.FilterSourceIdsWithUnreversedPostingsAsync(
            StockSourceType.Order,
            [200, 999],
            T0.AddMinutes(5),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new long[] { 200 }, result);
    }

    [Fact]
    public async Task FilterSourceIdsWithUnreversedPostingsAsync_ShouldReturnEmpty_WhenCandidateSetIsEmpty()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);

        // Act
        var result = await ledger.FilterSourceIdsWithUnreversedPostingsAsync(
            StockSourceType.Order,
            [],
            T0,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task FilterSourceIdsWithUnreversedPostingsAsync_ShouldReturnDistinctIdsInAscendingOrder()
    {
        // Arrange — order 300 has two placement lines, and orders are posted out of id order.
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        host.DateTime.UtcNow = T0;
        await SeedStockAsync(ledger, 1, 50);
        await PlaceAsync(ledger, orderId: 300, lineId: 1);
        await PlaceAsync(ledger, orderId: 300, lineId: 2);
        await PlaceAsync(ledger, orderId: 100);
        await PlaceAsync(ledger, orderId: 200);

        // Act
        var result = await ledger.FilterSourceIdsWithUnreversedPostingsAsync(
            StockSourceType.Order,
            [300, 200, 100],
            T0.AddMinutes(5),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new long[] { 100, 200, 300 }, result);
    }

    [Fact]
    public async Task ReverseOrderPlacementsAsync_ShouldReverseOnlyOriginalsAtOrBeforeTheCutoff_AndLeaveNewerUntouched()
    {
        // Arrange — line 1 was posted before the cutoff, line 2 after it.
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        host.DateTime.UtcNow = T0;
        await SeedStockAsync(ledger, 1, 10);
        await PlaceAsync(ledger, orderId: 100, quantity: 3, lineId: 1);
        host.DateTime.UtcNow = T0.AddMinutes(10);
        await PlaceAsync(ledger, orderId: 100, quantity: 2, lineId: 2);

        // Act
        await ledger.ReverseOrderPlacementsAsync(
            100,
            "system:stock-reconciliation",
            TestContext.Current.CancellationToken,
            postedBefore: T0.AddMinutes(5));

        // Assert — only the older 3 units are restored: 10 - 3 - 2 + 3 = 8.
        var level = await host.Context.StockLevels.FirstAsync(x => x.ProductId == 1, TestContext.Current.CancellationToken);
        Assert.Equal(8, level.QuantityOnHand);

        var reversals = await host.Context.StockAdjustments
            .Where(x => x.Reason == StockAdjustmentReason.OrderCancellationRestore)
            .ToListAsync(TestContext.Current.CancellationToken);
        var reversal = Assert.Single(reversals);
        Assert.Equal(3, reversal.QuantityDelta);

        var newer = await host.Context.StockAdjustments.SingleAsync(
            x => x.Reason == StockAdjustmentReason.OrderPlacement && x.SourceLineId == 2,
            TestContext.Current.CancellationToken);
        Assert.Equal("order-place:100:2", newer.IdempotencyKey);
        Assert.False(await host.Context.StockAdjustments.AnyAsync(
            x => x.ReversesAdjustmentId == newer.Id,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReverseOrderPlacementsAsync_ShouldReverseEveryOriginal_WhenNoCutoffIsGiven()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        host.DateTime.UtcNow = T0;
        await SeedStockAsync(ledger, 1, 10);
        await PlaceAsync(ledger, orderId: 100, quantity: 3, lineId: 1);
        host.DateTime.UtcNow = T0.AddMinutes(10);
        await PlaceAsync(ledger, orderId: 100, quantity: 2, lineId: 2);

        // Act
        await ledger.ReverseOrderPlacementsAsync(
            100,
            "user-2",
            TestContext.Current.CancellationToken,
            postedBefore: null);

        // Assert
        var level = await host.Context.StockLevels.FirstAsync(x => x.ProductId == 1, TestContext.Current.CancellationToken);
        Assert.Equal(10, level.QuantityOnHand);
        var reversalCount = await host.Context.StockAdjustments.CountAsync(
            x => x.Reason == StockAdjustmentReason.OrderCancellationRestore,
            TestContext.Current.CancellationToken);
        Assert.Equal(2, reversalCount);
    }

    [Fact]
    public async Task ReverseOrderPlacementsAsync_ShouldBeANoOp_WhenCalledASecondTimeWithTheSameCutoff()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        host.DateTime.UtcNow = T0;
        await SeedStockAsync(ledger, 1, 10);
        await PlaceAsync(ledger, orderId: 100, quantity: 3);
        var cutoff = T0.AddMinutes(5);
        await ledger.ReverseOrderPlacementsAsync(100, "user-2", TestContext.Current.CancellationToken, cutoff);

        // Act
        await ledger.ReverseOrderPlacementsAsync(100, "user-2", TestContext.Current.CancellationToken, cutoff);

        // Assert
        var level = await host.Context.StockLevels.FirstAsync(x => x.ProductId == 1, TestContext.Current.CancellationToken);
        Assert.Equal(10, level.QuantityOnHand);
        var reversalCount = await host.Context.StockAdjustments.CountAsync(
            x => x.Reason == StockAdjustmentReason.OrderCancellationRestore,
            TestContext.Current.CancellationToken);
        Assert.Equal(1, reversalCount);
    }
}
