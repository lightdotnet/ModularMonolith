using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Orders.Tests.TestSupport;
using StarterKit.Inventory.Contracts.Common;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Orders.Api.Application.Orders;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Orders.Contracts.Common;
using Xunit;

namespace Orders.Tests.Application.Orders;

/// <summary>
/// Exercises <see cref="OrphanedStockReconciliationService.ReconcileOnceAsync"/> against a real Sqlite
/// <see cref="StarterKit.Orders.Api.Data.OrdersDbContext"/>; only the cross-module
/// <see cref="IInventoryService"/> seam is mocked. The clock is fixed at <see cref="Now"/>, so with the
/// default <c>MinAgeMinutes</c> of 5 the cutoff is <see cref="Cutoff"/>. Timestamps are whole minutes
/// apart because the Sqlite test provider stores <c>DateTimeOffset</c> with one-second precision.
/// </summary>
public class OrphanedStockReconciliationServiceTests
{
    private const string PerformedBy = "system:stock-reconciliation";

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Cutoff = Now.AddMinutes(-5);

    private static readonly DateTimeOffset Old = Now.AddHours(-1);

    private static readonly DateTimeOffset Young = Now.AddMinutes(-2);

    private static OrphanedStockReconciliationOptions MakeOptions(int batchSize = 200) =>
        new()
        {
            MinAgeMinutes = 5,
            BatchSize = batchSize,
        };

    private static Mock<IInventoryService> MakeInventoryMock(params long[] idsWithStock)
    {
        var mock = new Mock<IInventoryService>();

        // Mirrors the real seam: returns the ascending subset of the candidates that hold stock.
        mock.Setup(x => x.FilterSourceIdsWithUnreversedPostingsAsync(
                StockSourceType.Order,
                It.IsAny<IReadOnlyCollection<long>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                StockSourceType _,
                IReadOnlyCollection<long> candidates,
                DateTimeOffset _,
                CancellationToken _) =>
                (IReadOnlyList<long>)candidates
                    .Where(idsWithStock.Contains)
                    .OrderBy(x => x)
                    .ToList());

        return mock;
    }

    // Persists an order stamped as created at the given time (Created comes from the audit clock).
    private static async Task<Order> SeedAsync(
        OrdersTestHost host,
        Order order,
        DateTimeOffset createdAt)
    {
        host.DateTime.UtcNow = createdAt;
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        host.DateTime.UtcNow = Now;

        return order;
    }

    private static async Task<int> ReconcileAsync(
        OrdersTestHost host,
        Mock<IInventoryService> inventoryMock,
        OrphanedStockReconciliationOptions? options = null,
        OrphanedStockReconciliationState? state = null)
    {
        host.DateTime.UtcNow = Now;

        return await OrphanedStockReconciliationService.ReconcileOnceAsync(
            host.Context,
            inventoryMock.Object,
            host.DateTime,
            options ?? MakeOptions(),
            state ?? new OrphanedStockReconciliationState(),
            NullLogger.Instance,
            TestContext.Current.CancellationToken);
    }

    private static void VerifyRestored(
        Mock<IInventoryService> inventoryMock,
        long orderId,
        Times times) =>
        inventoryMock.Verify(
            x => x.RestoreForOrderAsync(
                orderId,
                PerformedBy,
                It.IsAny<CancellationToken>(),
                It.IsAny<DateTimeOffset?>()),
            times);

    [Fact]
    public async Task ReconcileOnceAsync_ShouldRestoreAStaleDraftOrphan_WithTheSystemUserAndTheCutoff()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = await SeedAsync(host, OrderBuilder.DraftWithLine(), Old);
        var inventoryMock = MakeInventoryMock(order.Id);

        // Act
        var restored = await ReconcileAsync(host, inventoryMock);

        // Assert
        Assert.Equal(1, restored);
        inventoryMock.Verify(
            x => x.RestoreForOrderAsync(
                order.Id,
                PerformedBy,
                It.IsAny<CancellationToken>(),
                Cutoff),
            Times.Once);
        inventoryMock.Verify(
            x => x.FilterSourceIdsWithUnreversedPostingsAsync(
                StockSourceType.Order,
                It.Is<IReadOnlyCollection<long>>(ids => ids.Contains(order.Id)),
                Cutoff,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldFenceTheOrder_SoStockReconciledAtIsPersisted()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = await SeedAsync(host, OrderBuilder.DraftWithLine(), Old);
        var inventoryMock = MakeInventoryMock(order.Id);

        // Act
        await ReconcileAsync(host, inventoryMock);

        // Assert
        host.Context.ChangeTracker.Clear();
        var reloaded = await host.Context.Orders.FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(Now, reloaded.StockReconciledAt);
        Assert.Equal(OrderStatus.Draft, reloaded.Status);
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldNeverConsiderADraftYoungerThanTheCutoff()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = await SeedAsync(host, OrderBuilder.DraftWithLine(), Young);
        var inventoryMock = MakeInventoryMock(order.Id);

        // Act
        var restored = await ReconcileAsync(host, inventoryMock);

        // Assert
        Assert.Equal(0, restored);
        inventoryMock.Verify(
            x => x.FilterSourceIdsWithUnreversedPostingsAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<IReadOnlyCollection<long>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyRestored(inventoryMock, order.Id, Times.Never());
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldNeverConsiderAPlacedOrder()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = await SeedAsync(host, OrderBuilder.Placed(Old), Old);
        var inventoryMock = MakeInventoryMock(order.Id);

        // Act
        var restored = await ReconcileAsync(host, inventoryMock);

        // Assert
        Assert.Equal(0, restored);
        inventoryMock.Verify(
            x => x.FilterSourceIdsWithUnreversedPostingsAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<IReadOnlyCollection<long>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyRestored(inventoryMock, order.Id, Times.Never());
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldNeverConsiderAPlacedOrderThatWasLaterCancelled()
    {
        // Arrange — PlacedAt survives the cancel, so its stock is the cancel path's job, not the sweep's.
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Placed(Old);
        order.Cancel("user-1", "reason", Old.AddMinutes(1));
        await SeedAsync(host, order, Old);
        var inventoryMock = MakeInventoryMock(order.Id);

        // Act
        var restored = await ReconcileAsync(host, inventoryMock);

        // Assert
        Assert.Equal(0, restored);
        VerifyRestored(inventoryMock, order.Id, Times.Never());
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldRestoreACancelledOrderThatWasNeverPlaced()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.DraftWithLine();
        order.Cancel("user-1", "reason", Old);
        await SeedAsync(host, order, Old);
        var inventoryMock = MakeInventoryMock(order.Id);

        // Act
        var restored = await ReconcileAsync(host, inventoryMock);

        // Assert
        Assert.Equal(1, restored);
        VerifyRestored(inventoryMock, order.Id, Times.Once());
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldNotRestoreAnOrder_ThatInventoryDoesNotReturn()
    {
        // Arrange — Inventory reports only order B as still holding stock.
        using var host = new OrdersTestHost();
        var orderA = await SeedAsync(host, OrderBuilder.DraftWithLine(), Old);
        var orderB = await SeedAsync(host, OrderBuilder.DraftWithLine(), Old);
        var inventoryMock = MakeInventoryMock(orderB.Id);

        // Act
        var restored = await ReconcileAsync(host, inventoryMock);

        // Assert
        Assert.Equal(1, restored);
        VerifyRestored(inventoryMock, orderA.Id, Times.Never());
        VerifyRestored(inventoryMock, orderB.Id, Times.Once());

        host.Context.ChangeTracker.Clear();
        var reloadedA = await host.Context.Orders.FirstAsync(x => x.Id == orderA.Id, TestContext.Current.CancellationToken);
        Assert.Null(reloadedA.StockReconciledAt);
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldReturnZero_AndResetTheWatermark_WhenThereAreNoCandidates()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var inventoryMock = MakeInventoryMock();
        var state = new OrphanedStockReconciliationState { AfterOrderId = 42 };

        // Act
        var restored = await ReconcileAsync(host, inventoryMock, state: state);

        // Assert
        Assert.Equal(0, restored);
        Assert.Equal(0, state.AfterOrderId);
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldOnlyConsiderOrdersAfterTheWatermark()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var orderA = await SeedAsync(host, OrderBuilder.DraftWithLine(), Old);
        var orderB = await SeedAsync(host, OrderBuilder.DraftWithLine(), Old);
        var inventoryMock = MakeInventoryMock(orderA.Id, orderB.Id);
        var state = new OrphanedStockReconciliationState { AfterOrderId = orderA.Id };

        // Act
        var restored = await ReconcileAsync(host, inventoryMock, state: state);

        // Assert
        Assert.Equal(1, restored);
        VerifyRestored(inventoryMock, orderA.Id, Times.Never());
        VerifyRestored(inventoryMock, orderB.Id, Times.Once());
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldPageByBatchSize_AndResetTheWatermark_WhenTheLastPageIsShort()
    {
        // Arrange — five orders with a batch size of two gives pages of 2, 2 and 1.
        using var host = new OrdersTestHost();
        var orders = new List<Order>();
        for (var i = 0; i < 5; i++)
            orders.Add(await SeedAsync(host, OrderBuilder.DraftWithLine(), Old));

        var inventoryMock = MakeInventoryMock(orders.Select(x => x.Id).ToArray());
        var pageSizes = new List<int>();
        inventoryMock
            .Setup(x => x.FilterSourceIdsWithUnreversedPostingsAsync(
                StockSourceType.Order,
                It.IsAny<IReadOnlyCollection<long>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Callback((
                StockSourceType _,
                IReadOnlyCollection<long> candidates,
                DateTimeOffset _,
                CancellationToken _) => pageSizes.Add(candidates.Count))
            .ReturnsAsync((
                StockSourceType _,
                IReadOnlyCollection<long> candidates,
                DateTimeOffset _,
                CancellationToken _) => (IReadOnlyList<long>)candidates.OrderBy(x => x).ToList());
        var state = new OrphanedStockReconciliationState();

        // Act
        var restored = await ReconcileAsync(host, inventoryMock, MakeOptions(batchSize: 2), state);

        // Assert
        Assert.Equal(5, restored);
        Assert.Equal([2, 2, 1], pageSizes);
        Assert.Equal(0, state.AfterOrderId);
        foreach (var order in orders)
            VerifyRestored(inventoryMock, order.Id, Times.Once());
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldStopAfterMaxPagesPerTick_AndResumeFromTheWatermarkOnTheNextTick()
    {
        // Arrange — with a batch size of one, twelve orders need twelve pages; a tick handles ten.
        using var host = new OrdersTestHost();
        var orders = new List<Order>();
        for (var i = 0; i < 12; i++)
            orders.Add(await SeedAsync(host, OrderBuilder.DraftWithLine(), Old));

        var inventoryMock = MakeInventoryMock(orders.Select(x => x.Id).ToArray());
        var state = new OrphanedStockReconciliationState();
        var options = MakeOptions(batchSize: 1);

        // Act — first tick
        var firstTick = await ReconcileAsync(host, inventoryMock, options, state);

        // Assert — ten pages processed and the watermark points at the tenth order.
        Assert.Equal(10, firstTick);
        Assert.Equal(orders[9].Id, state.AfterOrderId);
        VerifyRestored(inventoryMock, orders[10].Id, Times.Never());

        // Act — second tick continues where the first stopped, then wraps.
        var secondTick = await ReconcileAsync(host, inventoryMock, options, state);

        // Assert
        Assert.Equal(2, secondTick);
        Assert.Equal(0, state.AfterOrderId);
        foreach (var order in orders)
            VerifyRestored(inventoryMock, order.Id, Times.Once());
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldAdvanceTheWatermarkOnlyAfterAPageIsProcessed_WhenTheInventoryFilterThrows()
    {
        // Arrange — page one succeeds (nothing to restore), page two's Inventory call throws.
        using var host = new OrdersTestHost();
        var orderA = await SeedAsync(host, OrderBuilder.DraftWithLine(), Old);
        await SeedAsync(host, OrderBuilder.DraftWithLine(), Old);
        var inventoryMock = new Mock<IInventoryService>();
        inventoryMock
            .SetupSequence(x => x.FilterSourceIdsWithUnreversedPostingsAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<IReadOnlyCollection<long>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>())
            .ThrowsAsync(new InvalidOperationException("Inventory unavailable"));
        var state = new OrphanedStockReconciliationState();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ReconcileAsync(host, inventoryMock, MakeOptions(batchSize: 1), state));

        // The failed page is retried on the next tick: the watermark stayed at the last processed page.
        Assert.Equal(orderA.Id, state.AfterOrderId);
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldNotAdvanceTheWatermark_WhenTheFirstInventoryFilterCallThrows()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await SeedAsync(host, OrderBuilder.DraftWithLine(), Old);
        var inventoryMock = new Mock<IInventoryService>();
        inventoryMock
            .Setup(x => x.FilterSourceIdsWithUnreversedPostingsAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<IReadOnlyCollection<long>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Inventory unavailable"));
        var state = new OrphanedStockReconciliationState();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => ReconcileAsync(host, inventoryMock, state: state));
        Assert.Equal(0, state.AfterOrderId);
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldSkipAnOrderWithoutRestoring_WhenTheFenceSaveHitsAConcurrencyConflict()
    {
        // Arrange — both orders are tracked by the context; order A's stored token is then changed
        // behind the context's back (as a concurrent Place/Cancel would), so its fence save conflicts.
        using var host = new OrdersTestHost();
        var orderA = await SeedAsync(host, OrderBuilder.DraftWithLine(), Old);
        var orderB = await SeedAsync(host, OrderBuilder.DraftWithLine(), Old);
        await host.Context.Database.ExecuteSqlRawAsync(
            "UPDATE \"Orders\" SET \"ConcurrencyToken\" = 'changed-elsewhere' WHERE \"Id\" = {0}",
            [orderA.Id],
            TestContext.Current.CancellationToken);
        var inventoryMock = MakeInventoryMock(orderA.Id, orderB.Id);

        // Act
        var restored = await ReconcileAsync(host, inventoryMock);

        // Assert — the conflicted order is skipped, the other one is still handled.
        Assert.Equal(1, restored);
        VerifyRestored(inventoryMock, orderA.Id, Times.Never());
        VerifyRestored(inventoryMock, orderB.Id, Times.Once());
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldKeepGoing_WhenOneOrdersRestoreThrows()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var orderA = await SeedAsync(host, OrderBuilder.DraftWithLine(), Old);
        var orderB = await SeedAsync(host, OrderBuilder.DraftWithLine(), Old);
        var inventoryMock = MakeInventoryMock(orderA.Id, orderB.Id);
        inventoryMock
            .Setup(x => x.RestoreForOrderAsync(
                orderA.Id,
                PerformedBy,
                It.IsAny<CancellationToken>(),
                It.IsAny<DateTimeOffset?>()))
            .ThrowsAsync(new InvalidOperationException("Inventory unavailable"));

        // Act
        var restored = await ReconcileAsync(host, inventoryMock);

        // Assert — only the successful restore is counted; the failure did not stop order B.
        Assert.Equal(1, restored);
        VerifyRestored(inventoryMock, orderA.Id, Times.Once());
        VerifyRestored(inventoryMock, orderB.Id, Times.Once());
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldPersistTheFence_BeforeCallingRestore()
    {
        // Arrange — inside the restore call, read the stored row to see whether the fence is already saved.
        using var host = new OrdersTestHost();
        var order = await SeedAsync(host, OrderBuilder.DraftWithLine(), Old);
        var inventoryMock = MakeInventoryMock(order.Id);
        DateTimeOffset? fenceSeenDuringRestore = null;
        inventoryMock
            .Setup(x => x.RestoreForOrderAsync(
                order.Id,
                PerformedBy,
                It.IsAny<CancellationToken>(),
                It.IsAny<DateTimeOffset?>()))
            .Returns(async (
                long id,
                string _,
                CancellationToken ct,
                DateTimeOffset? _) =>
            {
                fenceSeenDuringRestore = await host.Context.Orders
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => x.StockReconciledAt)
                    .FirstAsync(ct);
            });

        // Act
        var restored = await ReconcileAsync(host, inventoryMock);

        // Assert
        Assert.Equal(1, restored);
        Assert.Equal(Now, fenceSeenDuringRestore);
    }
}
