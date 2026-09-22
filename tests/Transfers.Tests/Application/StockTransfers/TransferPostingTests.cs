using Microsoft.EntityFrameworkCore;
using Light.Exceptions;
using Moq;
using StarterKit.Inventory.Contracts.Common;
using StarterKit.Inventory.Contracts.Exceptions;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Inventory.Contracts.Stock;
using StarterKit.Transfers.Api.Application.StockTransfers;
using StarterKit.Transfers.Api.Domain.StockTransfers;
using StarterKit.Transfers.Contracts.Common;
using Transfers.Tests.TestSupport;
using ValidationException = Light.Exceptions.ValidationException;

namespace Transfers.Tests.Application.StockTransfers;

/// <summary>
/// <see cref="TransferPosting"/> against a real Sqlite <c>TransfersDbContext</c>; only the cross-module
/// <see cref="IInventoryService"/> seam is mocked.
/// </summary>
public class TransferPostingTests
{
    private const string User = "user-1";

    private static Mock<IInventoryService> MakeInventory(
        bool anyLanded = false)
    {
        var mock = new Mock<IInventoryService>();

        mock.Setup(x => x.FilterSourceIdsWithUnreversedPostingsAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<IReadOnlyCollection<long>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                StockSourceType _,
                IReadOnlyCollection<long> ids,
                DateTimeOffset _,
                CancellationToken _) =>
                anyLanded
                    ? (IReadOnlyList<long>)ids.ToList()
                    : (IReadOnlyList<long>)[]);

        return mock;
    }

    private static IReadOnlyList<StockPostingResult> ResultsFor(
        StockTransfer transfer,
        decimal unitCost) =>
        transfer.Lines
            .Select(x => new StockPostingResult(
                x.ProductId,
                x.Id,
                x.RequestedQuantity,
                unitCost,
                unitCost * x.RequestedQuantity))
            .ToList();

    private static Task<PostingOutcome> DispatchAsync(
        TransfersTestHost host,
        IInventoryService inventory,
        StockTransfer transfer) =>
        TransferPosting.DispatchAsync(
            host.Context,
            inventory,
            transfer,
            User,
            host.DateTime,
            TestContext.Current.CancellationToken);

    private static Task<PostingOutcome> ReceiveAsync(
        TransfersTestHost host,
        IInventoryService inventory,
        StockTransfer transfer,
        TransferReceipt receipt,
        bool landed = false) =>
        TransferPosting.ReceiveAsync(
            host.Context,
            inventory,
            transfer,
            receipt,
            User,
            host.DateTime,
            landed,
            TestContext.Current.CancellationToken);

    // Dispatch

    [Fact]
    public async Task DispatchAsync_ShouldIssueStockAtTheSource_AndFreezeUnitCosts_OnSuccess()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.PostingAsync(host, null, (1, 10), (2, 4));
        var inventory = MakeInventory();
        inventory
            .Setup(x => x.IssueStockAsync(
                StockSourceType.Transfer,
                transfer.Id,
                TransferBuilder.Source,
                It.IsAny<IReadOnlyList<StockOutLine>>(),
                User,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultsFor(transfer, 7.5m));

        // Act
        var outcome = await DispatchAsync(host, inventory.Object, transfer);

        // Assert
        Assert.Equal(PostingOutcomeKind.Completed, outcome.Kind);
        Assert.Null(outcome.Failure);

        TransferSeed.Detach(host);
        var stored = await host.Context.StockTransfers
            .Include(x => x.Lines)
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(TransferStatus.Dispatched, stored.Status);
        Assert.All(stored.Lines, x => Assert.Equal(7.5m, x.UnitCostBase));
        Assert.All(stored.Lines, x => Assert.Equal(x.RequestedQuantity, x.QtyDispatched));

        inventory.Verify(
            x => x.IssueStockAsync(
                StockSourceType.Transfer,
                transfer.Id,
                TransferBuilder.Source,
                It.Is<IReadOnlyList<StockOutLine>>(lines =>
                    lines.Count == 2
                    && lines.All(l => l.IdempotencyRef == l.SourceLineId.ToString())),
                User,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DispatchAsync_ShouldAbortToDraft_WhenInventoryRefuses_AndNothingLanded(bool insufficient)
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.PostingAsync(host);
        Exception failure = insufficient
            ? new InsufficientStockException("Insufficient stock")
            : new ValidationException(new Dictionary<string, string[]> { ["location"] = ["gone"] });
        var inventory = MakeInventory(anyLanded: false);
        inventory
            .Setup(x => x.IssueStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockOutLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);

        // Act
        var outcome = await DispatchAsync(host, inventory.Object, transfer);

        // Assert
        Assert.Equal(PostingOutcomeKind.Refused, outcome.Kind);
        Assert.Same(failure, outcome.Failure);

        TransferSeed.Detach(host);
        var stored = await host.Context.StockTransfers.SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(TransferStatus.Draft, stored.Status);
        Assert.Null(stored.PostingStartedAt);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DispatchAsync_ShouldRollForward_WhenPostingsLanded_DespiteARefusalStyleFailure(bool insufficient)
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.PostingAsync(host);
        Exception failure = insufficient
            ? new InsufficientStockException("Insufficient stock")
            : new ValidationException(new Dictionary<string, string[]> { ["x"] = ["y"] });
        var inventory = MakeInventory(anyLanded: true);
        inventory
            .SetupSequence(x => x.IssueStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockOutLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure)
            .ReturnsAsync(ResultsFor(transfer, 3m));

        // Act
        var outcome = await DispatchAsync(host, inventory.Object, transfer);

        // Assert
        Assert.Equal(PostingOutcomeKind.Completed, outcome.Kind);
        Assert.Equal(TransferStatus.Dispatched, transfer.Status);
        Assert.All(transfer.Lines, x => Assert.Equal(3m, x.UnitCostBase));
        inventory.Verify(
            x => x.IssueStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockOutLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task DispatchAsync_ShouldAskInventoryWhetherPostingsLanded_AsOfNow()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.PostingAsync(host);
        var inventory = MakeInventory();
        inventory
            .Setup(x => x.IssueStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockOutLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InsufficientStockException("no"));

        // Act
        await DispatchAsync(host, inventory.Object, transfer);

        // Assert — "now", not a grace cutoff, so recent postings are seen.
        inventory.Verify(
            x => x.FilterSourceIdsWithUnreversedPostingsAsync(
                StockSourceType.Transfer,
                It.Is<IReadOnlyCollection<long>>(ids => ids.Single() == transfer.Id),
                host.DateTime.UtcNow,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ShouldLeaveThePosting_WhenAConcurrencyConflictIsTransient()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.PostingAsync(host);
        var failure = new ConflictException("Stock was modified concurrently");
        var inventory = MakeInventory(anyLanded: false);
        inventory
            .Setup(x => x.IssueStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockOutLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);

        // Act
        var outcome = await DispatchAsync(host, inventory.Object, transfer);

        // Assert
        Assert.Equal(PostingOutcomeKind.Pending, outcome.Kind);
        Assert.Same(failure, outcome.Failure);
        Assert.Equal(TransferStatus.Posting, transfer.Status);

        TransferSeed.Detach(host);
        Assert.Equal(
            TransferStatus.Posting,
            (await host.Context.StockTransfers.SingleAsync(TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task DispatchAsync_ShouldPropagate_AnUnexpectedException_AndLeaveThePostingUntouched()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.PostingAsync(host);
        var inventory = MakeInventory();
        inventory
            .Setup(x => x.IssueStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockOutLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => DispatchAsync(host, inventory.Object, transfer));
        Assert.Equal(TransferStatus.Posting, transfer.Status);
    }

    [Theory]
    [InlineData(typeof(InsufficientStockException), true)]
    [InlineData(typeof(ValidationException), true)]
    [InlineData(typeof(ConflictException), false)]
    [InlineData(typeof(InvalidOperationException), false)]
    public void IsRefusal_ShouldOnlyBeTrue_ForShortageOrValidationFailures(
        Type exceptionType,
        bool expected)
    {
        Exception exception = exceptionType == typeof(ValidationException)
            ? new ValidationException(new Dictionary<string, string[]> { ["x"] = ["y"] })
            : (Exception)Activator.CreateInstance(exceptionType, "m")!;

        Assert.Equal(expected, TransferPosting.IsRefusal(exception));
    }

    // Receive

    [Fact]
    public async Task ReceiveAsync_ShouldReceiveAtTheDestination_AtFrozenCost_AndApplyTheReceipt()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host, (1, 10), (2, 4));
        var line1 = transfer.Lines.Single(x => x.ProductId == 1);
        var receipt = await TransferSeed.ReceivingAsync(host, transfer, "req-1", (line1.Id, 6));
        var inventory = MakeInventory();

        // Act
        var outcome = await ReceiveAsync(host, inventory.Object, transfer, receipt);

        // Assert
        Assert.Equal(PostingOutcomeKind.Completed, outcome.Kind);
        Assert.Equal(TransferStatus.PartiallyReceived, transfer.Status);
        Assert.Equal(TransferReceiptStatus.Posted, receipt.Status);
        Assert.Equal(6, line1.QtyReceived);

        inventory.Verify(
            x => x.ReceiveStockAsync(
                StockSourceType.TransferReceipt,
                receipt.Id,
                TransferBuilder.Destination,
                It.Is<IReadOnlyList<StockInLine>>(lines =>
                    lines.Count == 1
                    && lines[0].ProductId == 1
                    && lines[0].SourceLineId == line1.Id
                    && lines[0].Quantity == 6
                    && lines[0].UnitCostBase == 5m),
                User,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReceiveAsync_ShouldVoidTheReceipt_WhenInventoryRefusesAndNothingLanded()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host);
        var receipt = await TransferSeed.ReceivingAsync(host, transfer, "req-1", (transfer.Lines[0].Id, 3));
        var failure = new ValidationException(new Dictionary<string, string[]> { ["location"] = ["missing"] });
        var inventory = MakeInventory(anyLanded: false);
        inventory
            .Setup(x => x.ReceiveStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockInLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);

        // Act
        var outcome = await ReceiveAsync(host, inventory.Object, transfer, receipt);

        // Assert
        Assert.Equal(PostingOutcomeKind.Refused, outcome.Kind);
        Assert.Same(failure, outcome.Failure);

        TransferSeed.Detach(host);
        var stored = await host.Context.TransferReceipts.SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(TransferReceiptStatus.Voided, stored.Status);
        Assert.StartsWith("Inventory refused the receipt:", stored.VoidReason);
        Assert.True(stored.VoidReason!.Length <= 1000);
        Assert.Equal(host.DateTime.UtcNow, stored.VoidedAt);

        var storedTransfer = await host.Context.StockTransfers
            .Include(x => x.Lines)
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(TransferStatus.Dispatched, storedTransfer.Status);
        Assert.All(storedTransfer.Lines, x => Assert.Equal(0, x.QtyReceived));
    }

    [Fact]
    public async Task ReceiveAsync_ShouldApplyTheReceipt_WhenValidationFailsButPostingsLanded()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host);
        var receipt = await TransferSeed.ReceivingAsync(host, transfer, "req-1", (transfer.Lines[0].Id, 3));
        var inventory = MakeInventory(anyLanded: true);
        inventory
            .Setup(x => x.ReceiveStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockInLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(new Dictionary<string, string[]> { ["x"] = ["y"] }));

        // Act
        var outcome = await ReceiveAsync(host, inventory.Object, transfer, receipt);

        // Assert
        Assert.Equal(PostingOutcomeKind.Completed, outcome.Kind);
        Assert.Equal(TransferReceiptStatus.Posted, receipt.Status);
        Assert.Equal(3, transfer.Lines[0].QtyReceived);
    }

    [Fact]
    public async Task ReceiveAsync_ShouldNotCallInventory_WhenThePostingsAreAlreadyKnownToHaveLanded()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host);
        var receipt = await TransferSeed.ReceivingAsync(host, transfer, "req-1", (transfer.Lines[0].Id, 10));
        var inventory = MakeInventory();

        // Act
        var outcome = await ReceiveAsync(host, inventory.Object, transfer, receipt, landed: true);

        // Assert
        Assert.Equal(PostingOutcomeKind.Completed, outcome.Kind);
        Assert.Equal(TransferStatus.Received, transfer.Status);
        inventory.Verify(
            x => x.ReceiveStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockInLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReceiveAsync_ShouldLeaveTheReceiptPosting_AndPropagate_ATransientConflict()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host);
        var receipt = await TransferSeed.ReceivingAsync(host, transfer, "req-1", (transfer.Lines[0].Id, 3));
        var inventory = MakeInventory();
        inventory
            .Setup(x => x.ReceiveStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockInLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("Stock was modified concurrently"));

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => ReceiveAsync(host, inventory.Object, transfer, receipt));
        Assert.Equal(TransferReceiptStatus.Posting, receipt.Status);
        Assert.Equal(TransferStatus.Dispatched, transfer.Status);
    }

    [Fact]
    public async Task ReceiveAsync_ShouldTruncateAnOverlongVoidReason_To1000Characters()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host);
        var receipt = await TransferSeed.ReceivingAsync(host, transfer, "req-1", (transfer.Lines[0].Id, 3));
        var inventory = MakeInventory(anyLanded: false);
        inventory
            .Setup(x => x.ReceiveStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockInLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(new Dictionary<string, string[]> { ["x"] = [new string('e', 3000)] }));

        // Act
        await ReceiveAsync(host, inventory.Object, transfer, receipt);

        // Assert
        Assert.Equal(TransferReceiptStatus.Voided, receipt.Status);
        Assert.True(receipt.VoidReason!.Length <= 1000);
    }
}
