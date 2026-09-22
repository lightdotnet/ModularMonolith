using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Purchasing.Tests.TestSupport;
using StarterKit.Inventory.Contracts.Common;
using StarterKit.Inventory.Contracts.Exceptions;
using StarterKit.Purchasing.Api.Application.Posting;
using ValidationException = Light.Exceptions.ValidationException;

namespace Purchasing.Tests.Application.Posting;

/// <summary>
/// <see cref="PurchasingPostingReconciliationService.ReconcileOnceAsync"/> against a real Sqlite
/// <c>PurchasingDbContext</c>; only <c>IInventoryService</c> is mocked. The clock is fixed at
/// <see cref="Now"/>, so with <c>StuckAfterMinutes</c> = 5 the cutoff is 11:55 and the poison cutoff (10x)
/// is 11:10. Timestamps are whole minutes apart because Sqlite stores <c>DateTimeOffset</c> with one-second
/// precision.
/// </summary>
public class PurchasingPostingReconciliationServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Old = Now.AddMinutes(-30);

    private static readonly DateTimeOffset VeryOld = Now.AddHours(-2);

    private static readonly DateTimeOffset Young = Now.AddMinutes(-2);

    private static PurchasingPostingReconciliationOptions MakeOptions(int batchSize = 200) =>
        new()
        {
            StuckAfterMinutes = 5,
            BatchSize = batchSize,
        };

    private static async Task<int> ReconcileAsync(
        PurchasingTestHost host,
        Mock<StarterKit.Inventory.Contracts.Services.IInventoryService> inventory,
        PurchasingPostingReconciliationOptions? options = null,
        PurchasingPostingReconciliationState? state = null,
        ILogger? logger = null)
    {
        host.DateTime.UtcNow = Now;
        PurchasingSeed.Detach(host);

        return await PurchasingPostingReconciliationService.ReconcileOnceAsync(
            host.Context,
            inventory.Object,
            host.DateTime,
            options ?? MakeOptions(),
            state ?? new PurchasingPostingReconciliationState(),
            logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance,
            Ct);
    }

    // Seeds an approved order with a receipt still Posting since the given time (audit Created).
    private static async Task<(long ReceiptId, long OrderId)> SeedReceivingAsync(
        PurchasingTestHost host,
        DateTimeOffset createdAt,
        int quantity = 4)
    {
        host.DateTime.UtcNow = Now;
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));

        host.DateTime.UtcNow = createdAt;
        var receipt = await PurchasingSeed.ReceivingAsync(host, order, null, (order.Lines[0].Id, quantity));
        host.DateTime.UtcNow = Now;

        return (receipt.Id, order.Id);
    }

    // Seeds a return stuck in Posting since the given time.
    private static async Task<long> SeedReturnPostingAsync(
        PurchasingTestHost host,
        DateTimeOffset postingStartedAt)
    {
        host.DateTime.UtcNow = Now;
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));
        var receipt = await PurchasingSeed.PostedReceiptAsync(host, order, (order.Lines[0].Id, 6));
        var purchaseReturn = await PurchasingSeed.ReturnPostingAsync(host, receipt, postingStartedAt, (receipt.Lines[0].Id, 2));

        return purchaseReturn.Id;
    }

    private static async Task<GoodsReceiptStatus> ReceiptStatusAsync(
        PurchasingTestHost host,
        long id)
    {
        PurchasingSeed.Detach(host);

        return await host.Context.GoodsReceipts.Where(x => x.Id == id).Select(x => x.Status).SingleAsync(Ct);
    }

    private static async Task<PurchaseReturnStatus> ReturnStatusAsync(
        PurchasingTestHost host,
        long id)
    {
        PurchasingSeed.Detach(host);

        return await host.Context.PurchaseReturns.Where(x => x.Id == id).Select(x => x.Status).SingleAsync(Ct);
    }

    // Receipts

    [Fact]
    public async Task ReconcileOnceAsync_ShouldOnlyPickUpReceiptsStuckPastTheGracePeriod()
    {
        using var host = new PurchasingTestHost();
        var (stuck, _) = await SeedReceivingAsync(host, Old);
        var (young, _) = await SeedReceivingAsync(host, Young);
        var inventory = InventoryMock.Create();

        var resolved = await ReconcileAsync(host, inventory);

        Assert.Equal(1, resolved);
        Assert.Equal(GoodsReceiptStatus.Posted, await ReceiptStatusAsync(host, stuck));
        Assert.Equal(GoodsReceiptStatus.Posting, await ReceiptStatusAsync(host, young));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldReceiveAsTheRecordedUser_WhenNothingLandedYet()
    {
        using var host = new PurchasingTestHost();
        var (receiptId, orderId) = await SeedReceivingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: false);

        var resolved = await ReconcileAsync(host, inventory);

        Assert.Equal(1, resolved);
        inventory.Verify(
            x => x.ReceiveStockAsync(
                StockSourceType.GoodsReceipt,
                receiptId,
                PurchasingBuilder.Location,
                It.IsAny<IReadOnlyList<StarterKit.Inventory.Contracts.Stock.StockInLine>>(),
                "receiver",
                It.IsAny<CancellationToken>()),
            Times.Once);
        var order = await host.Context.PurchaseOrders.Include(x => x.Lines).SingleAsync(x => x.Id == orderId, Ct);
        Assert.Equal(4, order.TotalReceivedQuantity);
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldApplyALandedReceipt_WithoutCallingInventoryAgain()
    {
        using var host = new PurchasingTestHost();
        var (receiptId, _) = await SeedReceivingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: true);

        var resolved = await ReconcileAsync(host, inventory);

        Assert.Equal(1, resolved);
        inventory.VerifyReceiveCalls(Times.Never());
        Assert.Equal(GoodsReceiptStatus.Posted, await ReceiptStatusAsync(host, receiptId));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldVoidAReceipt_WhenInventoryRefusesItAndNothingLanded()
    {
        using var host = new PurchasingTestHost();
        var (receiptId, orderId) = await SeedReceivingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupReceiveThrows(new ValidationException(new Dictionary<string, string[]> { ["l"] = ["gone"] }));

        var resolved = await ReconcileAsync(host, inventory);

        Assert.Equal(1, resolved);
        Assert.Equal(GoodsReceiptStatus.Voided, await ReceiptStatusAsync(host, receiptId));
        var order = await host.Context.PurchaseOrders.Include(x => x.Lines).SingleAsync(x => x.Id == orderId, Ct);
        Assert.Equal(0, order.TotalReceivedQuantity);
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldRetryAReceiptLater_OnATransientConflict()
    {
        using var host = new PurchasingTestHost();
        var (receiptId, _) = await SeedReceivingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupReceiveThrows(new ConflictException("Stock was modified concurrently"));

        var resolved = await ReconcileAsync(host, inventory);

        Assert.Equal(0, resolved);
        Assert.Equal(GoodsReceiptStatus.Posting, await ReceiptStatusAsync(host, receiptId));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldSkipAReceipt_ThatWasModifiedConcurrently()
    {
        using var host = new PurchasingTestHost();
        var (receiptId, _) = await SeedReceivingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: true);
        // Every commit-2 attempt loses the race: the sweep reports the stock-recorded conflict and moves on.
        host.SaveFaults.Arm(failTimes: 3);

        var resolved = await ReconcileAsync(host, inventory);

        Assert.Equal(0, resolved);
        Assert.Equal(GoodsReceiptStatus.Posting, await ReceiptStatusAsync(host, receiptId));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldContinueWithTheOtherReceipts_WhenOneFails()
    {
        using var host = new PurchasingTestHost();
        var (first, _) = await SeedReceivingAsync(host, Old);
        var (second, _) = await SeedReceivingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory
            .Setup(x => x.ReceiveStockAsync(
                StockSourceType.GoodsReceipt,
                first,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StarterKit.Inventory.Contracts.Stock.StockInLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var resolved = await ReconcileAsync(host, inventory);

        Assert.Equal(1, resolved);
        Assert.Equal(GoodsReceiptStatus.Posting, await ReceiptStatusAsync(host, first));
        Assert.Equal(GoodsReceiptStatus.Posted, await ReceiptStatusAsync(host, second));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldPageReceiptsByKeyset_AndAdvanceOrResetTheWatermark()
    {
        // Arrange — 12 stuck receipts that stay stuck; batch size 1 caps a tick at 10 pages.
        using var host = new PurchasingTestHost();
        var ids = new List<long>();
        for (var i = 0; i < 12; i++)
            ids.Add((await SeedReceivingAsync(host, Old)).ReceiptId);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupReceiveThrows(new ConflictException("Stock was modified concurrently"));
        var state = new PurchasingPostingReconciliationState();
        var options = MakeOptions(batchSize: 1);

        // Act & Assert
        await ReconcileAsync(host, inventory, options, state);

        inventory.VerifyReceiveCalls(Times.Exactly(10));
        Assert.Equal(ids.OrderBy(x => x).ElementAt(9), state.AfterReceiptId);

        await ReconcileAsync(host, inventory, options, state);

        inventory.VerifyReceiveCalls(Times.Exactly(12));
        Assert.Equal(0, state.AfterReceiptId);
    }

    // Returns

    [Fact]
    public async Task ReconcileOnceAsync_ShouldOnlyPickUpReturnsStuckPastTheGracePeriod_AndFinishThem()
    {
        using var host = new PurchasingTestHost();
        var stuck = await SeedReturnPostingAsync(host, Old);
        var young = await SeedReturnPostingAsync(host, Young);
        var inventory = InventoryMock.Create();
        inventory.SetupIssueAtCost(7m);

        var resolved = await ReconcileAsync(host, inventory);

        Assert.Equal(1, resolved);
        Assert.Equal(PurchaseReturnStatus.Posted, await ReturnStatusAsync(host, stuck));
        Assert.Equal(PurchaseReturnStatus.Posting, await ReturnStatusAsync(host, young));
        inventory.Verify(
            x => x.IssueStockAsync(
                StockSourceType.PurchaseReturn,
                stuck,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StarterKit.Inventory.Contracts.Stock.StockOutLine>>(),
                "poster",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReconcileOnceAsync_ShouldAbortAReturnToDraft_WhenInventoryRefusesAndNothingLanded(bool insufficient)
    {
        using var host = new PurchasingTestHost();
        var stuck = await SeedReturnPostingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupIssueThrows(insufficient
            ? new InsufficientStockException("Insufficient stock")
            : new ValidationException(new Dictionary<string, string[]> { ["x"] = ["y"] }));

        var resolved = await ReconcileAsync(host, inventory);

        Assert.Equal(1, resolved);
        Assert.Equal(PurchaseReturnStatus.Draft, await ReturnStatusAsync(host, stuck));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldRollAReturnForward_WhenPostingsLanded_AndNeverReverse()
    {
        using var host = new PurchasingTestHost();
        var stuck = await SeedReturnPostingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: true);
        inventory.SetupIssueAtCost(3m);

        var resolved = await ReconcileAsync(host, inventory);

        Assert.Equal(1, resolved);
        Assert.Equal(PurchaseReturnStatus.Posted, await ReturnStatusAsync(host, stuck));
        inventory.Verify(
            x => x.RestoreForOrderAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<DateTimeOffset?>()),
            Times.Never);
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldRetryAReturnLater_OnATransientConflict()
    {
        using var host = new PurchasingTestHost();
        var stuck = await SeedReturnPostingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupIssueThrows(new ConflictException("Stock was modified concurrently"));

        var resolved = await ReconcileAsync(host, inventory);

        Assert.Equal(0, resolved);
        Assert.Equal(PurchaseReturnStatus.Posting, await ReturnStatusAsync(host, stuck));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldPageReturnsByKeyset_AndResetTheWatermark()
    {
        using var host = new PurchasingTestHost();
        for (var i = 0; i < 3; i++)
            await SeedReturnPostingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupIssueThrows(new ConflictException("Stock was modified concurrently"));
        var state = new PurchasingPostingReconciliationState { AfterReturnId = 0 };

        await ReconcileAsync(host, inventory, MakeOptions(batchSize: 2), state);

        inventory.VerifyIssueCalls(Times.Exactly(3));
        Assert.Equal(0, state.AfterReturnId);
    }

    // Logging: poison documents

    [Fact]
    public async Task ReconcileOnceAsync_ShouldLogAWarning_ForAStuckDocumentWithinTenGracePeriods()
    {
        using var host = new PurchasingTestHost();
        await SeedReturnPostingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupIssueThrows(new ConflictException("Stock was modified concurrently"));
        var logger = new CapturingLogger();

        await ReconcileAsync(host, inventory, logger: logger);

        Assert.DoesNotContain(logger.Entries, x => x.Level == LogLevel.Error);
        Assert.Contains(logger.Entries, x => x.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldLogAnError_ForAReturnStuckLongerThanTenGracePeriods()
    {
        using var host = new PurchasingTestHost();
        await SeedReturnPostingAsync(host, VeryOld);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupIssueThrows(new ConflictException("Stock was modified concurrently"));
        var logger = new CapturingLogger();

        await ReconcileAsync(host, inventory, logger: logger);

        Assert.Contains(logger.Entries, x => x.Level == LogLevel.Error && x.Message.Contains("manual attention"));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldLogAnError_ForAReceiptStuckLongerThanTenGracePeriods()
    {
        using var host = new PurchasingTestHost();
        await SeedReceivingAsync(host, VeryOld);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupReceiveThrows(new ConflictException("Stock was modified concurrently"));
        var logger = new CapturingLogger();

        await ReconcileAsync(host, inventory, logger: logger);

        Assert.Contains(logger.Entries, x => x.Level == LogLevel.Error && x.Message.Contains("manual attention"));
    }

    // Options

    [Theory]
    [InlineData(0, 5, 200, false)]
    [InlineData(5, 1, 200, false)]
    [InlineData(5, 2, 0, false)]
    [InlineData(1, 2, 1, true)]
    public void Options_ShouldValidateRanges(
        int interval,
        int stuckAfter,
        int batch,
        bool expected)
    {
        var options = new PurchasingPostingReconciliationOptions { IntervalMinutes = interval, StuckAfterMinutes = stuckAfter, BatchSize = batch };

        Assert.Equal(
            expected,
            System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
                options,
                new System.ComponentModel.DataAnnotations.ValidationContext(options),
                null,
                true));
    }
}
