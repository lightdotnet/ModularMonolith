using Inventory.Tests.TestSupport;
using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using StarterKit.Inventory.Api.Domain.StockAdjustments;
using StarterKit.Inventory.Api.Domain.StockLevels;
using StarterKit.Inventory.Api.Services;
using StarterKit.Inventory.Contracts.Common;
using Xunit;

namespace Inventory.Tests.Services;

/// <summary>
/// Covers how <see cref="StockLedger"/> prices each movement (moving-average cost) against a real Sqlite
/// <see cref="StarterKit.Inventory.Api.Data.InventoryDbContext"/>.
/// </summary>
public class StockLedgerPricingTests
{
    private const string LocationId = "location-1";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static StockMovement Manual(
        int quantity,
        decimal? unitCost = null,
        long productId = 1) =>
        new(
            productId,
            LocationId,
            quantity,
            StockAdjustmentReason.ManualAdjustment,
            "user-1",
            UnitCostBase: unitCost);

    private static StockMovement Revaluation(
        decimal unitCost,
        long productId = 1) =>
        new(
            productId,
            LocationId,
            0,
            StockAdjustmentReason.CostRevaluation,
            "user-1",
            UnitCostBase: unitCost);

    private static StockMovement Placement(
        long orderId,
        int quantity,
        long lineId = 1,
        long productId = 1) =>
        new(
            productId,
            LocationId,
            -quantity,
            StockAdjustmentReason.OrderPlacement,
            "user-1",
            SourceType: StockSourceType.Order,
            SourceId: orderId,
            SourceLineId: lineId,
            IdempotencyKey: $"order-place:{orderId}:{lineId}");

    private static async Task<StockLevel> LevelAsync(
        InventoryTestHost host,
        long productId = 1) =>
        await host.Context.StockLevels
            .AsNoTracking()
            .SingleAsync(x => x.ProductId == productId && x.LocationId == LocationId, Ct);

    private static async Task<List<StockAdjustment>> AdjustmentsAsync(InventoryTestHost host) =>
        await host.Context.StockAdjustments
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(Ct);

    [Fact]
    public async Task ApplyAsync_ShouldPriceAnInboundAtTheGivenCost_AndUpdateTheAverage()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);

        // Act
        await ledger.ApplyAsync([Manual(10, 2m)], Ct);
        await ledger.ApplyAsync([Manual(10, 4m)], Ct);

        // Assert
        var level = await LevelAsync(host);
        Assert.Equal(20, level.QuantityOnHand);
        Assert.Equal(60m, level.TotalValueBase);
        Assert.Equal(3m, level.AverageCostBase);

        var adjustments = await AdjustmentsAsync(host);
        Assert.Equal(2m, adjustments[0].UnitCostBase);
        Assert.Equal(20m, adjustments[0].ValueDeltaBase);
        Assert.Equal(4m, adjustments[1].UnitCostBase);
        Assert.Equal(40m, adjustments[1].ValueDeltaBase);
    }

    [Fact]
    public async Task ApplyAsync_ShouldRoundTheInboundValueToFourDecimals()
    {
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);

        await ledger.ApplyAsync([Manual(3, 1.23456m)], Ct);

        var level = await LevelAsync(host);
        Assert.Equal(3.7037m, level.TotalValueBase);
    }

    [Fact]
    public async Task ApplyAsync_ShouldPriceAManualInboundWithoutACostAtTheCurrentAverage()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        await ledger.ApplyAsync([Manual(10, 2m)], Ct);
        await ledger.ApplyAsync([Manual(10, 4m)], Ct);

        // Act
        await ledger.ApplyAsync([Manual(5)], Ct);

        // Assert
        var level = await LevelAsync(host);
        Assert.Equal(25, level.QuantityOnHand);
        Assert.Equal(75m, level.TotalValueBase);
        Assert.Equal(3m, level.AverageCostBase);

        var last = (await AdjustmentsAsync(host)).Last();
        Assert.Equal(3m, last.UnitCostBase);
        Assert.Equal(15m, last.ValueDeltaBase);
    }

    [Fact]
    public async Task ApplyAsync_ShouldRejectAnInboundWithoutACost_WhenNoLevelExists_AndPersistNothing()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => ledger.ApplyAsync([Manual(5)], Ct));

        Assert.False(await host.Context.StockLevels.AnyAsync(Ct));
        Assert.False(await host.Context.StockAdjustments.AnyAsync(Ct));
    }

    [Fact]
    public async Task ApplyAsync_ShouldRejectAnInboundWithoutACost_WhenTheLevelIsEmpty()
    {
        // Arrange — the level row exists but was emptied.
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        await ledger.ApplyAsync([Manual(5, 2m)], Ct);
        await ledger.ApplyAsync([Manual(-5)], Ct);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => ledger.ApplyAsync([Manual(3)], Ct));

        var level = await LevelAsync(host);
        Assert.Equal(0, level.QuantityOnHand);
        Assert.Equal(0m, level.TotalValueBase);
        Assert.Equal(2, await host.Context.StockAdjustments.CountAsync(Ct));
    }

    [Fact]
    public async Task ApplyAsync_ShouldAllowAZeroCostInbound_WhenNothingIsOnHand()
    {
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);

        await ledger.ApplyAsync([Manual(5, 0m)], Ct);

        var level = await LevelAsync(host);
        Assert.Equal(5, level.QuantityOnHand);
        Assert.Equal(0m, level.TotalValueBase);
    }

    [Fact]
    public async Task ApplyAsync_ShouldPriceAnOutboundAtTheAverage_AndKeepTheAverageUnchanged()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        await ledger.ApplyAsync([Manual(10, 3m)], Ct);

        // Act
        await ledger.ApplyAsync([Manual(-4)], Ct);

        // Assert
        var level = await LevelAsync(host);
        Assert.Equal(6, level.QuantityOnHand);
        Assert.Equal(18m, level.TotalValueBase);
        Assert.Equal(3m, level.AverageCostBase);

        var outbound = (await AdjustmentsAsync(host)).Last();
        Assert.Equal(3m, outbound.UnitCostBase);
        Assert.Equal(-12m, outbound.ValueDeltaBase);
    }

    [Fact]
    public async Task ApplyAsync_ShouldIgnoreAGivenCost_ForAnOutbound()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        await ledger.ApplyAsync([Manual(10, 3m)], Ct);

        // Act — the cost on an outbound movement is not used: it always leaves at the average.
        await ledger.ApplyAsync([Manual(-2, unitCost: 99m)], Ct);

        // Assert
        var outbound = (await AdjustmentsAsync(host)).Last();
        Assert.Equal(-6m, outbound.ValueDeltaBase);
    }

    [Fact]
    public async Task ApplyAsync_ShouldTakeTheWholeRemainingValue_WhenTheLevelIsEmptied()
    {
        // Arrange — 3 units worth exactly 10 (1 @ 1 + 2 @ 4.5); issuing them one by one rounds each step.
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        await ledger.ApplyAsync([Manual(1, 1m)], Ct);
        await ledger.ApplyAsync([Manual(2, 4.5m)], Ct);

        // Act
        await ledger.ApplyAsync([Manual(-1)], Ct);
        await ledger.ApplyAsync([Manual(-1)], Ct);
        await ledger.ApplyAsync([Manual(-1)], Ct);

        // Assert
        var level = await LevelAsync(host);
        Assert.Equal(0, level.QuantityOnHand);
        Assert.Equal(0m, level.TotalValueBase);

        var outbound = (await AdjustmentsAsync(host)).Skip(2).Select(x => x.ValueDeltaBase).ToList();
        Assert.Equal(new[] { -3.3333m, -3.3334m, -3.3333m }, outbound);
    }

    [Fact]
    public async Task ReverseOrderPlacementsAsync_ShouldReverseTheOriginalsValueExactly_EvenAfterTheAverageChanged()
    {
        // Arrange — the order leaves at an average of 2, then a dearer receipt lifts the average to 7.
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        await ledger.ApplyAsync([Manual(10, 2m)], Ct);
        await ledger.ApplyAsync([Placement(orderId: 1, quantity: 4)], Ct);
        await ledger.ApplyAsync([Manual(10, 10m)], Ct);
        Assert.Equal(7m, (await LevelAsync(host)).AverageCostBase);

        // Act
        await ledger.ReverseOrderPlacementsAsync(1, "user-2", Ct);

        // Assert — the restore carries the original -8 back as +8, not 4 x the new average of 7.
        var restore = await host.Context.StockAdjustments
            .AsNoTracking()
            .SingleAsync(x => x.Reason == StockAdjustmentReason.OrderCancellationRestore, Ct);
        Assert.Equal(4, restore.QuantityDelta);
        Assert.Equal(2m, restore.UnitCostBase);
        Assert.Equal(8m, restore.ValueDeltaBase);

        var level = await LevelAsync(host);
        Assert.Equal(20, level.QuantityOnHand);
        Assert.Equal(120m, level.TotalValueBase);
    }

    [Fact]
    public async Task ApplyAsync_ShouldSetTheValueToOnHandTimesTheNewCost_AndRecordTheDelta_ForARevaluation()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        await ledger.ApplyAsync([Manual(10, 2m)], Ct);

        // Act
        await ledger.ApplyAsync([Revaluation(3m)], Ct);
        await ledger.ApplyAsync([Revaluation(1m)], Ct);

        // Assert
        var level = await LevelAsync(host);
        Assert.Equal(10, level.QuantityOnHand);
        Assert.Equal(10m, level.TotalValueBase);
        Assert.Equal(1m, level.AverageCostBase);

        var revaluations = (await AdjustmentsAsync(host)).Skip(1).ToList();
        Assert.All(revaluations, x =>
        {
            Assert.Equal(StockAdjustmentReason.CostRevaluation, x.Reason);
            Assert.Equal(0, x.QuantityDelta);
        });
        Assert.Equal(new[] { 3m, 1m }, revaluations.Select(x => x.UnitCostBase));
        Assert.Equal(new[] { 10m, -20m }, revaluations.Select(x => x.ValueDeltaBase));
    }

    [Fact]
    public async Task ApplyAsync_ShouldRejectARevaluation_WhenNoLevelExists()
    {
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);

        await Assert.ThrowsAsync<ValidationException>(() => ledger.ApplyAsync([Revaluation(3m)], Ct));

        Assert.False(await host.Context.StockLevels.AnyAsync(Ct));
        Assert.False(await host.Context.StockAdjustments.AnyAsync(Ct));
    }

    [Fact]
    public async Task ApplyAsync_ShouldRejectARevaluation_WhenTheLevelIsEmpty()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        await ledger.ApplyAsync([Manual(5, 2m)], Ct);
        await ledger.ApplyAsync([Manual(-5)], Ct);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => ledger.ApplyAsync([Revaluation(3m)], Ct));

        Assert.Equal(2, await host.Context.StockAdjustments.CountAsync(Ct));
    }

    [Fact]
    public async Task ApplyAsync_ShouldRejectARevaluation_WhenTheNewValueRoundsToZeroWithStockOnHand()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        await ledger.ApplyAsync([Manual(1, 1m)], Ct);

        // Act & Assert — 1 x 0.00001 rounds to 0.0000, which would look like an emptied level.
        await Assert.ThrowsAsync<ValidationException>(() => ledger.ApplyAsync([Revaluation(0.00001m)], Ct));

        var level = await LevelAsync(host);
        Assert.Equal(1, level.QuantityOnHand);
        Assert.Equal(1m, level.TotalValueBase);
        Assert.Equal(1, await host.Context.StockAdjustments.CountAsync(Ct));
    }

    [Fact]
    public async Task ApplyAsync_ShouldKeepTotalValueEqualToTheSumOfValueDeltas_AfterAMixedSequence()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);

        // Act
        await ledger.ApplyAsync([Manual(7, 1.3333m)], Ct);
        await ledger.ApplyAsync([Manual(5, 2.7777m)], Ct);
        await ledger.ApplyAsync([Placement(orderId: 1, quantity: 3)], Ct);
        await ledger.ApplyAsync([Manual(-2)], Ct);
        await ledger.ApplyAsync([Revaluation(1.9999m)], Ct);
        await ledger.ApplyAsync([Manual(4)], Ct);
        await ledger.ReverseOrderPlacementsAsync(1, "user-2", Ct);
        await ledger.ApplyAsync([Manual(-3)], Ct);
        await ledger.ApplyAsync([Revaluation(2.5m)], Ct);
        await ledger.ApplyAsync([Manual(-11)], Ct);

        // Assert — everything issued again: the value must have drained to exactly zero.
        var level = await LevelAsync(host);
        var adjustments = await AdjustmentsAsync(host);
        Assert.Equal(adjustments.Sum(x => x.QuantityDelta), level.QuantityOnHand);
        Assert.Equal(adjustments.Sum(x => x.ValueDeltaBase), level.TotalValueBase);
        Assert.Equal(0, level.QuantityOnHand);
        Assert.Equal(0m, level.TotalValueBase);
    }

    [Fact]
    public async Task ApplyAsync_ShouldKeepTotalValueEqualToTheSumOfValueDeltas_WhileStockRemains()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);

        // Act
        await ledger.ApplyAsync([Manual(9, 1.1111m)], Ct);
        await ledger.ApplyAsync([Manual(-4)], Ct);
        await ledger.ApplyAsync([Revaluation(3.3333m)], Ct);
        await ledger.ApplyAsync([Manual(6)], Ct);
        await ledger.ApplyAsync([Manual(-7)], Ct);

        // Assert
        var level = await LevelAsync(host);
        var adjustments = await AdjustmentsAsync(host);
        Assert.Equal(4, level.QuantityOnHand);
        Assert.True(level.TotalValueBase > 0m);
        Assert.Equal(adjustments.Sum(x => x.ValueDeltaBase), level.TotalValueBase);
    }

    [Fact]
    public async Task ApplyAsync_ShouldClearTheChangeTracker_WhenPricingThrowsMidBatch()
    {
        // Arrange — product 1 is mutated first, then product 2 (no level, no cost) fails to price.
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        await ledger.ApplyAsync([Manual(5, 2m, productId: 1)], Ct);

        // Act
        await Assert.ThrowsAsync<ValidationException>(() => ledger.ApplyAsync(
            [Manual(3, 1m, productId: 1), Manual(4, unitCost: null, productId: 2)],
            Ct));

        // Assert — no half-applied entity is left tracked, so a later save persists nothing of it.
        Assert.Empty(host.Context.ChangeTracker.Entries());

        await host.Context.SaveChangesAsync(Ct);

        var level = await LevelAsync(host, productId: 1);
        Assert.Equal(5, level.QuantityOnHand);
        Assert.Equal(10m, level.TotalValueBase);
        Assert.False(await host.Context.StockLevels.AnyAsync(x => x.ProductId == 2, Ct));
        Assert.Equal(1, await host.Context.StockAdjustments.CountAsync(Ct));
    }

    [Fact]
    public async Task ApplyAsync_ShouldClearTheChangeTracker_WhenARevaluationIsRejectedForAnUnknownLevel()
    {
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);

        await Assert.ThrowsAsync<ValidationException>(() => ledger.ApplyAsync([Revaluation(3m)], Ct));

        Assert.Empty(host.Context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ApplyAsync_ShouldClearTheChangeTracker_WhenAShortageIsReported()
    {
        // Arrange
        using var host = new InventoryTestHost();
        var ledger = new StockLedger(host.Context, host.DateTime);
        await ledger.ApplyAsync([Manual(2, 2m)], Ct);

        // Act & Assert
        await Assert.ThrowsAsync<StarterKit.Inventory.Contracts.Exceptions.InsufficientStockException>(() => ledger.ApplyAsync([Manual(-5)], Ct));

        Assert.Empty(host.Context.ChangeTracker.Entries());
        Assert.Equal(2, (await LevelAsync(host)).QuantityOnHand);
    }
}
