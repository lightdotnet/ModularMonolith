using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StarterKit.Inventory.Contracts.Common;
using StarterKit.Inventory.Contracts.Exceptions;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Inventory.Contracts.Stock;
using StarterKit.Transfers.Api.Application.StockTransfers;
using StarterKit.Transfers.Contracts.Common;
using Transfers.Tests.TestSupport;
using ValidationException = Light.Exceptions.ValidationException;

namespace Transfers.Tests.Application.StockTransfers;

/// <summary>
/// <see cref="TransfersPostingReconciliationService.ReconcileOnceAsync"/> against a real Sqlite
/// <c>TransfersDbContext</c>; only <see cref="IInventoryService"/> is mocked. The clock is fixed at
/// <see cref="Now"/>, so with <c>StuckAfterMinutes</c> = 5 the cutoff is 11:55. Timestamps are whole
/// minutes apart because Sqlite stores <c>DateTimeOffset</c> with one-second precision.
/// </summary>
public class TransfersPostingReconciliationServiceTests
{
    private const string PerformedBy = "system:transfers-reconciliation";

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Old = Now.AddHours(-1);

    private static readonly DateTimeOffset Young = Now.AddMinutes(-2);

    private static TransfersPostingReconciliationOptions MakeOptions(int batchSize = 200) =>
        new()
        {
            StuckAfterMinutes = 5,
            BatchSize = batchSize,
        };

    private static async Task<int> ReconcileAsync(
        TransfersTestHost host,
        Mock<IInventoryService> inventory,
        TransfersPostingReconciliationOptions? options = null,
        TransfersPostingReconciliationState? state = null)
    {
        host.DateTime.UtcNow = Now;
        TransferSeed.Detach(host);

        return await TransfersPostingReconciliationService.ReconcileOnceAsync(
            host.Context,
            inventory.Object,
            host.DateTime,
            options ?? MakeOptions(),
            state ?? new TransfersPostingReconciliationState(),
            NullLogger.Instance,
            TestContext.Current.CancellationToken);
    }

    private static async Task<TransferStatus> StatusOfAsync(
        TransfersTestHost host,
        long id)
    {
        TransferSeed.Detach(host);

        return await host.Context.StockTransfers
            .Where(x => x.Id == id)
            .Select(x => x.Status)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<TransferReceiptStatus> ReceiptStatusAsync(
        TransfersTestHost host,
        long id)
    {
        TransferSeed.Detach(host);

        return await host.Context.TransferReceipts
            .Where(x => x.Id == id)
            .Select(x => x.Status)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<StarterKit.Transfers.Api.Domain.StockTransfers.StockTransfer> SeedPostingAsync(
        TransfersTestHost host,
        DateTimeOffset postingStartedAt)
    {
        host.DateTime.UtcNow = postingStartedAt;
        var transfer = await TransferSeed.PostingAsync(host, postingStartedAt);
        host.DateTime.UtcNow = Now;

        return transfer;
    }

    // Seeds a dispatched transfer with one Posting receipt created at the given time.
    private static async Task<(StarterKit.Transfers.Api.Domain.StockTransfers.StockTransfer Transfer, long ReceiptId)> SeedReceivingAsync(
        TransfersTestHost host,
        DateTimeOffset receiptCreatedAt,
        int quantity = 3)
    {
        host.DateTime.UtcNow = Now;
        var transfer = await TransferSeed.DispatchedAsync(host, (1, 10));

        host.DateTime.UtcNow = receiptCreatedAt;
        var receipt = await TransferSeed.ReceivingAsync(host, transfer, $"req-{Guid.NewGuid():N}", (transfer.Lines[0].Id, quantity));
        host.DateTime.UtcNow = Now;

        return (transfer, receipt.Id);
    }

    // Dispatches

    [Fact]
    public async Task ReconcileOnceAsync_ShouldOnlyPickUpTransfersStuckPastTheGracePeriod()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var stuck = await SeedPostingAsync(host, Old);
        var young = await SeedPostingAsync(host, Young);
        var draft = await TransferSeed.DraftAsync(host);
        var inventory = InventoryMock.Create();
        inventory.SetupIssueAtCost(4m);

        // Act
        var resolved = await ReconcileAsync(host, inventory);

        // Assert
        Assert.Equal(1, resolved);
        Assert.Equal(TransferStatus.Dispatched, await StatusOfAsync(host, stuck.Id));
        Assert.Equal(TransferStatus.Posting, await StatusOfAsync(host, young.Id));
        Assert.Equal(TransferStatus.Draft, await StatusOfAsync(host, draft.Id));
        inventory.VerifyIssueCalls(Times.Once());
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldIssueAsTheSystemUser_AndNeverReverseAnything()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var stuck = await SeedPostingAsync(host, Old);
        var inventory = InventoryMock.Create();
        inventory.SetupIssueAtCost(4m);

        // Act
        await ReconcileAsync(host, inventory);

        // Assert
        inventory.Verify(
            x => x.IssueStockAsync(
                StockSourceType.Transfer,
                stuck.Id,
                TransferBuilder.Source,
                It.IsAny<IReadOnlyList<StockOutLine>>(),
                PerformedBy,
                It.IsAny<CancellationToken>()),
            Times.Once);
        inventory.Verify(
            x => x.RestoreForOrderAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<DateTimeOffset?>()),
            Times.Never);
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldRollADispatchForward_WhenPostingsLanded()
    {
        // Arrange — Inventory reports the postings as landed and the idempotent issue replays their costs.
        using var host = new TransfersTestHost();
        var stuck = await SeedPostingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: true);
        inventory.SetupIssueAtCost(6m);

        // Act
        var resolved = await ReconcileAsync(host, inventory);

        // Assert
        Assert.Equal(1, resolved);
        TransferSeed.Detach(host);
        var stored = await host.Context.StockTransfers.Include(x => x.Lines).SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(TransferStatus.Dispatched, stored.Status);
        Assert.All(stored.Lines, x => Assert.Equal(6m, x.UnitCostBase));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReconcileOnceAsync_ShouldAbortADispatchToDraft_WhenInventoryRefusesAndNothingLanded(bool insufficient)
    {
        // Arrange
        using var host = new TransfersTestHost();
        var stuck = await SeedPostingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupIssueThrows(insufficient
            ? new InsufficientStockException("Insufficient stock")
            : new ValidationException(new Dictionary<string, string[]> { ["x"] = ["y"] }));

        // Act
        var resolved = await ReconcileAsync(host, inventory);

        // Assert
        Assert.Equal(1, resolved);
        Assert.Equal(TransferStatus.Draft, await StatusOfAsync(host, stuck.Id));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldRetryLater_WhenTheFailureIsATransientConflict()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var stuck = await SeedPostingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupIssueThrows(new ConflictException("Stock was modified concurrently"));

        // Act
        var resolved = await ReconcileAsync(host, inventory);

        // Assert
        Assert.Equal(0, resolved);
        Assert.Equal(TransferStatus.Posting, await StatusOfAsync(host, stuck.Id));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldSkipATransfer_ThatWasModifiedConcurrently()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var stuck = await SeedPostingAsync(host, Old);
        var inventory = InventoryMock.Create();
        inventory.SetupIssueAtCost(4m);
        host.SaveFaults.Arm(failWithConcurrency: true);

        // Act
        var resolved = await ReconcileAsync(host, inventory);

        // Assert
        Assert.Equal(0, resolved);
        Assert.Equal(TransferStatus.Posting, await StatusOfAsync(host, stuck.Id));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldContinueWithTheOthers_WhenOneTransferFails()
    {
        // Arrange — the first stuck transfer blows up unexpectedly, the second must still be finished.
        using var host = new TransfersTestHost();
        var first = await SeedPostingAsync(host, Old);
        var second = await SeedPostingAsync(host, Old);
        var inventory = InventoryMock.Create();
        inventory
            .Setup(x => x.IssueStockAsync(
                StockSourceType.Transfer,
                first.Id,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockOutLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        inventory
            .Setup(x => x.IssueStockAsync(
                StockSourceType.Transfer,
                second.Id,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockOutLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                StockSourceType _,
                long _,
                string _,
                IReadOnlyList<StockOutLine> lines,
                string _,
                CancellationToken _) =>
                (IReadOnlyList<StockPostingResult>)lines
                    .Select(x => new StockPostingResult(x.ProductId, x.SourceLineId, x.Quantity, 1m, x.Quantity))
                    .ToList());

        // Act
        var resolved = await ReconcileAsync(host, inventory);

        // Assert
        Assert.Equal(1, resolved);
        Assert.Equal(TransferStatus.Posting, await StatusOfAsync(host, first.Id));
        Assert.Equal(TransferStatus.Dispatched, await StatusOfAsync(host, second.Id));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldPageByKeyset_VisitingEachStuckTransferOnce_AndResetTheWatermark()
    {
        // Arrange — 5 transfers that stay stuck (transient conflict), batch size 2 -> pages of 2, 2, 1.
        using var host = new TransfersTestHost();
        for (var i = 0; i < 5; i++)
            await SeedPostingAsync(host, Old);
        var inventory = InventoryMock.Create();
        inventory.SetupIssueThrows(new ConflictException("Stock was modified concurrently"));
        var state = new TransfersPostingReconciliationState();

        // Act
        await ReconcileAsync(host, inventory, MakeOptions(batchSize: 2), state);

        // Assert — a short final page ends the pass: watermark reset, no transfer visited twice.
        inventory.VerifyIssueCalls(Times.Exactly(5));
        Assert.Equal(0, state.AfterTransferId);
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldAdvanceTheWatermarkAfterProcessing_AndResumeOnTheNextTick()
    {
        // Arrange — 12 stuck transfers, batch size 1: a tick is capped at 10 pages, so 2 remain.
        using var host = new TransfersTestHost();
        var ids = new List<long>();
        for (var i = 0; i < 12; i++)
            ids.Add((await SeedPostingAsync(host, Old)).Id);
        var inventory = InventoryMock.Create();
        inventory.SetupIssueThrows(new ConflictException("Stock was modified concurrently"));
        var state = new TransfersPostingReconciliationState();
        var options = MakeOptions(batchSize: 1);

        // Act
        await ReconcileAsync(host, inventory, options, state);

        // Assert — resumes after the last processed id instead of starting over.
        inventory.VerifyIssueCalls(Times.Exactly(10));
        Assert.Equal(ids.OrderBy(x => x).ElementAt(9), state.AfterTransferId);

        await ReconcileAsync(host, inventory, options, state);

        inventory.VerifyIssueCalls(Times.Exactly(12));
        Assert.Equal(0, state.AfterTransferId);
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldResetTheWatermark_WhenNothingIsLeftAfterIt()
    {
        using var host = new TransfersTestHost();
        await SeedPostingAsync(host, Old);
        var state = new TransfersPostingReconciliationState { AfterTransferId = 999, AfterReceiptId = 999 };

        var resolved = await ReconcileAsync(host, InventoryMock.Create(), state: state);

        Assert.Equal(0, resolved);
        Assert.Equal(0, state.AfterTransferId);
        Assert.Equal(0, state.AfterReceiptId);
    }

    // Receipts

    [Fact]
    public async Task ReconcileOnceAsync_ShouldOnlyPickUpReceiptsStuckPastTheGracePeriod()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var (_, stuckReceiptId) = await SeedReceivingAsync(host, Old);
        var (_, youngReceiptId) = await SeedReceivingAsync(host, Young);
        var inventory = InventoryMock.Create();

        // Act
        var resolved = await ReconcileAsync(host, inventory);

        // Assert
        Assert.Equal(1, resolved);
        Assert.Equal(TransferReceiptStatus.Posted, await ReceiptStatusAsync(host, stuckReceiptId));
        Assert.Equal(TransferReceiptStatus.Posting, await ReceiptStatusAsync(host, youngReceiptId));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldApplyALandedReceipt_WithoutCallingInventoryAgain()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var (transfer, receiptId) = await SeedReceivingAsync(host, Old, quantity: 10);
        var inventory = InventoryMock.Create(anyLanded: true);

        // Act
        var resolved = await ReconcileAsync(host, inventory);

        // Assert
        Assert.Equal(1, resolved);
        inventory.VerifyReceiveCalls(Times.Never());
        Assert.Equal(TransferReceiptStatus.Posted, await ReceiptStatusAsync(host, receiptId));
        Assert.Equal(TransferStatus.Received, await StatusOfAsync(host, transfer.Id));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldReceiveAnUnlandedReceipt_AsTheSystemUser()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var (transfer, receiptId) = await SeedReceivingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: false);

        // Act
        var resolved = await ReconcileAsync(host, inventory);

        // Assert
        Assert.Equal(1, resolved);
        inventory.Verify(
            x => x.ReceiveStockAsync(
                StockSourceType.TransferReceipt,
                receiptId,
                TransferBuilder.Destination,
                It.IsAny<IReadOnlyList<StockInLine>>(),
                PerformedBy,
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(TransferStatus.PartiallyReceived, await StatusOfAsync(host, transfer.Id));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldVoidAReceipt_WhenInventoryRefusesItAndNothingLanded()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var (transfer, receiptId) = await SeedReceivingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupReceiveThrows(new ValidationException(new Dictionary<string, string[]> { ["location"] = ["missing"] }));

        // Act
        var resolved = await ReconcileAsync(host, inventory);

        // Assert
        Assert.Equal(1, resolved);
        Assert.Equal(TransferReceiptStatus.Voided, await ReceiptStatusAsync(host, receiptId));
        Assert.Equal(TransferStatus.Dispatched, await StatusOfAsync(host, transfer.Id));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldRetryAReceiptLater_WhenInventoryHitsATransientConflict()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var (_, receiptId) = await SeedReceivingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupReceiveThrows(new ConflictException("Stock was modified concurrently"));

        // Act
        var resolved = await ReconcileAsync(host, inventory);

        // Assert
        Assert.Equal(0, resolved);
        Assert.Equal(TransferReceiptStatus.Posting, await ReceiptStatusAsync(host, receiptId));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldSkipAReceipt_ThatWasModifiedConcurrently()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var (_, receiptId) = await SeedReceivingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: true);
        host.SaveFaults.Arm(failWithConcurrency: true);

        // Act
        var resolved = await ReconcileAsync(host, inventory);

        // Assert
        Assert.Equal(0, resolved);
        Assert.Equal(TransferReceiptStatus.Posting, await ReceiptStatusAsync(host, receiptId));
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldPageReceiptsByKeyset_AndResetTheWatermark()
    {
        // Arrange — three stuck receipts on separate transfers; transient failures keep them Posting.
        using var host = new TransfersTestHost();
        for (var i = 0; i < 3; i++)
            await SeedReceivingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupReceiveThrows(new ConflictException("Stock was modified concurrently"));
        var state = new TransfersPostingReconciliationState();

        // Act
        await ReconcileAsync(host, inventory, MakeOptions(batchSize: 2), state);

        // Assert
        inventory.VerifyReceiveCalls(Times.Exactly(3));
        Assert.Equal(0, state.AfterReceiptId);
    }

    [Fact]
    public async Task ReconcileOnceAsync_ShouldReconcileBothADispatchAndAReceipt_InOneSweep()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var stuckDispatch = await SeedPostingAsync(host, Old);
        var (_, receiptId) = await SeedReceivingAsync(host, Old);
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupIssueAtCost(2m);

        // Act
        var resolved = await ReconcileAsync(host, inventory);

        // Assert
        Assert.Equal(2, resolved);
        Assert.Equal(TransferStatus.Dispatched, await StatusOfAsync(host, stuckDispatch.Id));
        Assert.Equal(TransferReceiptStatus.Posted, await ReceiptStatusAsync(host, receiptId));
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
        int batchSize,
        bool expectedValid)
    {
        var options = new TransfersPostingReconciliationOptions
        {
            IntervalMinutes = interval,
            StuckAfterMinutes = stuckAfter,
            BatchSize = batchSize,
        };

        var valid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            options,
            new System.ComponentModel.DataAnnotations.ValidationContext(options),
            null,
            validateAllProperties: true);

        Assert.Equal(expectedValid, valid);
    }

    [Fact]
    public void Options_ShouldDefaultToAValidConfiguration()
    {
        var options = new TransfersPostingReconciliationOptions();

        Assert.True(options.Enabled);
        Assert.Equal(5, options.IntervalMinutes);
        Assert.Equal(5, options.StuckAfterMinutes);
        Assert.Equal(200, options.BatchSize);
        Assert.True(System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            options,
            new System.ComponentModel.DataAnnotations.ValidationContext(options),
            null,
            validateAllProperties: true));
    }
}
