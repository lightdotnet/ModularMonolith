using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;
using StarterKit.Transfers.Api.Application.StockTransfers;
using StarterKit.Transfers.Api.Application.StockTransfers.Commands;
using StarterKit.Transfers.Api.Domain.StockTransfers;
using StarterKit.Transfers.Contracts.Common;
using StarterKit.Transfers.Contracts.StockTransfers;
using Transfers.Tests.TestSupport;
using ValidationException = Light.Exceptions.ValidationException;

namespace Transfers.Tests.Application.StockTransfers.Commands;

public class ReceiveStockTransferCommandHandlerTests
{
    private static ReceiveStockTransferCommand Command(
        long transferId,
        string clientRequestId,
        params (long LineId, int Quantity)[] lines) =>
        new(
            transferId,
            new ReceiveStockTransferRequest
            {
                ClientRequestId = clientRequestId,
                Lines = lines
                    .Select(x => new ReceiveStockTransferLineRequest { TransferLineId = x.LineId, Quantity = x.Quantity })
                    .ToList(),
            },
            "receiver");

    private static ReceiveStockTransferCommandHandler MakeHandler(
        TransfersTestHost host,
        Mock<StarterKit.Inventory.Contracts.Services.IInventoryService> inventory) =>
        new(host.Context, inventory.Object, host.DateTime);

    private static async Task<StockTransfer> ReloadAsync(TransfersTestHost host)
    {
        TransferSeed.Detach(host);

        return await host.Context.StockTransfers
            .Include(x => x.Lines)
            .Include(x => x.Receipts)
                .ThenInclude(x => x.Lines)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTheTransferDoesNotExist()
    {
        using var host = new TransfersTestHost();
        var handler = MakeHandler(host, InventoryMock.Create());

        var result = await handler.Handle(Command(999, "r", (1, 1)), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReceive_PostAtTheFrozenCost_AndReturnTheReceiptId()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host, (1, 10));
        var inventory = InventoryMock.Create();
        var handler = MakeHandler(host, inventory);

        // Act
        var result = await handler.Handle(Command(transfer.Id, "req-1", (transfer.Lines[0].Id, 4)), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var stored = await ReloadAsync(host);
        var receipt = Assert.Single(stored.Receipts);
        Assert.Equal(receipt.Id, result.Data);
        Assert.Equal(TransferReceiptStatus.Posted, receipt.Status);
        Assert.Equal(TransferStatus.PartiallyReceived, stored.Status);
        Assert.Equal(4, stored.Lines[0].QtyReceived);
        inventory.VerifyReceiveCalls(Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldBecomeReceived_WhenTheLastQuantityArrives()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host, (1, 10));
        var handler = MakeHandler(host, InventoryMock.Create());
        var lineId = transfer.Lines[0].Id;

        await handler.Handle(Command(transfer.Id, "a", (lineId, 4)), TestContext.Current.CancellationToken);
        await handler.Handle(Command(transfer.Id, "b", (lineId, 6)), TestContext.Current.CancellationToken);

        var stored = await ReloadAsync(host);
        Assert.Equal(TransferStatus.Received, stored.Status);
        Assert.NotNull(stored.ReceivedAt);
    }

    [Fact]
    public async Task Handle_ShouldReturnTheExistingReceipt_WithoutReceivingTwice_ForADuplicateClientRequestId()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host, (1, 10));
        var inventory = InventoryMock.Create();
        var handler = MakeHandler(host, inventory);
        var lineId = transfer.Lines[0].Id;

        // Act
        var first = await handler.Handle(Command(transfer.Id, "req-1", (lineId, 4)), TestContext.Current.CancellationToken);
        var second = await handler.Handle(Command(transfer.Id, "  REQ-1 ", (lineId, 4)), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(first.Data, second.Data);
        inventory.VerifyReceiveCalls(Times.Once());
        var stored = await ReloadAsync(host);
        Assert.Single(stored.Receipts);
        Assert.Equal(4, stored.Lines[0].QtyReceived);
    }

    [Fact]
    public async Task Handle_ShouldFinishAReceiptLeftInPosting_WhenTheSameRequestIdIsResubmitted()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host, (1, 10));
        var receipt = await TransferSeed.ReceivingAsync(host, transfer, "req-1", (transfer.Lines[0].Id, 4));
        var inventory = InventoryMock.Create();
        var handler = MakeHandler(host, inventory);
        TransferSeed.Detach(host);

        // Act
        var result = await handler.Handle(Command(transfer.Id, "req-1", (transfer.Lines[0].Id, 4)), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(receipt.Id, result.Data);
        var stored = await ReloadAsync(host);
        Assert.Equal(TransferReceiptStatus.Posted, stored.Receipts.Single().Status);
        inventory.VerifyReceiveCalls(Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldThrowConflict_WhenResubmittingAVoidedReceipt()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host, (1, 10));
        var receipt = await TransferSeed.ReceivingAsync(host, transfer, "req-1", (transfer.Lines[0].Id, 4));
        transfer.VoidReceipt(receipt.Id, "refused", host.DateTime.UtcNow);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = MakeHandler(host, InventoryMock.Create());
        TransferSeed.Detach(host);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(Command(transfer.Id, "req-1", (transfer.Lines[0].Id, 4)), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldRejectOverReceipt_WithValidation_AndPostNothing()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host, (1, 10));
        var inventory = InventoryMock.Create();
        var handler = MakeHandler(host, inventory);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(Command(transfer.Id, "req-1", (transfer.Lines[0].Id, 11)), TestContext.Current.CancellationToken));

        inventory.VerifyReceiveCalls(Times.Never());
        Assert.Empty((await ReloadAsync(host)).Receipts);
    }

    [Fact]
    public async Task Handle_ShouldThrowConflict_WhenTheTransferIsNotReceivable()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host);
        var handler = MakeHandler(host, InventoryMock.Create());

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(Command(transfer.Id, "req-1", (transfer.Lines[0].Id, 1)), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldVoidTheReceipt_AndRethrowInventoryValidation_WhenNothingLanded()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host, (1, 10));
        var inventory = InventoryMock.Create(anyLanded: false);
        inventory.SetupReceiveThrows(new ValidationException(new Dictionary<string, string[]> { ["location"] = ["missing"] }));
        var handler = MakeHandler(host, inventory);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(Command(transfer.Id, "req-1", (transfer.Lines[0].Id, 4)), TestContext.Current.CancellationToken));

        var stored = await ReloadAsync(host);
        Assert.Equal(TransferReceiptStatus.Voided, stored.Receipts.Single().Status);
        Assert.Equal(TransferStatus.Dispatched, stored.Status);
        Assert.Equal(0, stored.Lines[0].QtyReceived);
    }

    [Fact]
    public async Task Handle_ShouldLeaveTheReceiptPosting_WhenInventoryHitsATransientConflict()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host, (1, 10));
        var inventory = InventoryMock.Create();
        inventory.SetupReceiveThrows(new ConflictException("Stock was modified concurrently"));
        var handler = MakeHandler(host, inventory);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(Command(transfer.Id, "req-1", (transfer.Lines[0].Id, 4)), TestContext.Current.CancellationToken));

        var stored = await ReloadAsync(host);
        Assert.Equal(TransferReceiptStatus.Posting, stored.Receipts.Single().Status);
    }

    [Fact]
    public async Task Handle_ShouldReturnTheWinnersReceipt_WhenAConcurrentSubmitOfTheSameRequestIdWinsTheInsertRace()
    {
        // Arrange — a competing writer records the same ClientRequestId just before our first commit.
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host, (1, 10));
        var lineId = transfer.Lines[0].Id;
        var inventory = InventoryMock.Create();
        var handler = MakeHandler(host, inventory);
        long winnerReceiptId = 0;

        host.SaveFaults.Arm(beforeSave: async () =>
        {
            using var other = host.NewContext();
            var competing = await other.StockTransfers
                .Include(x => x.Lines)
                .Include(x => x.Receipts)
                .SingleAsync(TestContext.Current.CancellationToken);

            var receipt = competing.BeginReceive("req-1", [(lineId, 4)], host.DateTime.UtcNow);

            await other.SaveChangesAsync(TestContext.Current.CancellationToken);

            winnerReceiptId = receipt.Id;
        });

        // Act
        var result = await handler.Handle(Command(transfer.Id, "req-1", (lineId, 4)), TestContext.Current.CancellationToken);

        // Assert — the loser adopts the winner's receipt and finishes it exactly once.
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0, winnerReceiptId);
        Assert.Equal(winnerReceiptId, result.Data);
        var stored = await ReloadAsync(host);
        Assert.Single(stored.Receipts);
        Assert.Equal(TransferReceiptStatus.Posted, stored.Receipts[0].Status);
        inventory.VerifyReceiveCalls(Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldMapAConcurrencyLossOnTheSecondCommit_ToConflict()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host, (1, 10));
        var handler = MakeHandler(host, InventoryMock.Create());
        host.SaveFaults.Arm(skipSaves: 1, failWithConcurrency: true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(Command(transfer.Id, "req-1", (transfer.Lines[0].Id, 4)), TestContext.Current.CancellationToken));

        Assert.Equal(TransferSaving.ConcurrencyMessage, ex.Message);
    }

    [Fact]
    public void Validator_ShouldRejectAnEmptyRequest()
    {
        var result = new ReceiveStockTransferCommandValidator().Validate(Command(1, "", (1, 1)) with { ReceivedByUserId = "" });

        Assert.False(result.IsValid);
    }
}
