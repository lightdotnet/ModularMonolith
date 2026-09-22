using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using StarterKit.Transfers.Api.Application.StockTransfers;
using StarterKit.Transfers.Api.Application.StockTransfers.Commands;
using StarterKit.Transfers.Api.Domain.StockTransfers;
using StarterKit.Transfers.Contracts.Common;
using StarterKit.Transfers.Contracts.StockTransfers;
using Transfers.Tests.TestSupport;

namespace Transfers.Tests.Application.StockTransfers.Commands;

public class CloseAndCancelStockTransferCommandHandlerTests
{
    private static CloseStockTransferCommand CloseCommand(
        long id,
        string reason = "lost in transit") =>
        new(id, new CloseStockTransferRequest { Reason = reason });

    private static CancelStockTransferCommand CancelCommand(
        long id,
        string reason = "not needed") =>
        new(id, new CancelStockTransferRequest { Reason = reason });

    private static async Task<StockTransfer> ReloadAsync(TransfersTestHost host)
    {
        TransferSeed.Detach(host);

        return await host.Context.StockTransfers
            .Include(x => x.Lines)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    // Close

    [Fact]
    public async Task Close_ShouldReturnNotFound_WhenTheTransferDoesNotExist()
    {
        using var host = new TransfersTestHost();
        var handler = new CloseStockTransferCommandHandler(host.Context, host.DateTime);

        var result = await handler.Handle(CloseCommand(999), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Close_ShouldWriteOffTheRemainder_AndPersistTheVarianceAtFrozenCost()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host, (1, 10), (2, 4));
        var handler = new CloseStockTransferCommandHandler(host.Context, host.DateTime);

        // Act
        var result = await handler.Handle(CloseCommand(transfer.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var stored = await ReloadAsync(host);
        Assert.Equal(TransferStatus.Closed, stored.Status);
        Assert.Equal("lost in transit", stored.ClosedReason);
        Assert.Equal(14, stored.Lines.Sum(x => x.QtyClosedShort));
        Assert.Equal(14 * 5m, stored.ClosedShortValueBase);
    }

    [Fact]
    public async Task Close_ShouldThrowConflict_WhenAReceiptIsStillPosting()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host, (1, 10));
        await TransferSeed.ReceivingAsync(host, transfer, "req-1", (transfer.Lines[0].Id, 3));
        var handler = new CloseStockTransferCommandHandler(host.Context, host.DateTime);
        TransferSeed.Detach(host);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(CloseCommand(transfer.Id), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Close_ShouldThrowConflict_FromADraft()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host);
        var handler = new CloseStockTransferCommandHandler(host.Context, host.DateTime);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(CloseCommand(transfer.Id), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Close_ShouldMapAConcurrencyLoss_ToConflict()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host);
        var handler = new CloseStockTransferCommandHandler(host.Context, host.DateTime);
        host.SaveFaults.Arm(failWithConcurrency: true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(CloseCommand(transfer.Id), TestContext.Current.CancellationToken));

        Assert.Equal(TransferSaving.ConcurrencyMessage, ex.Message);
    }

    [Fact]
    public async Task Close_ShouldMapARealConcurrencyLoss_ToConflict()
    {
        // Arrange — another writer rotates the concurrency token between load and save.
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host);
        var handler = new CloseStockTransferCommandHandler(host.Context, host.DateTime);
        host.SaveFaults.Arm(beforeSave: async () =>
        {
            using var other = host.NewContext();
            var competing = await other.StockTransfers.Include(x => x.Lines).Include(x => x.Receipts).SingleAsync(TestContext.Current.CancellationToken);
            competing.Close("closed by someone else", host.DateTime.UtcNow);
            await other.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(CloseCommand(transfer.Id), TestContext.Current.CancellationToken));

        Assert.Equal(TransferSaving.ConcurrencyMessage, ex.Message);
    }

    [Theory]
    [InlineData(0, "r", false)]
    [InlineData(1, "", false)]
    [InlineData(1, "r", true)]
    public void CloseValidator_ShouldRequireIdAndReason(
        long id,
        string reason,
        bool expected)
    {
        var result = new CloseStockTransferCommandValidator().Validate(CloseCommand(id, reason));

        Assert.Equal(expected, result.IsValid);
    }

    // Cancel

    [Fact]
    public async Task Cancel_ShouldReturnNotFound_WhenTheTransferDoesNotExist()
    {
        using var host = new TransfersTestHost();
        var handler = new CancelStockTransferCommandHandler(host.Context, host.DateTime);

        var result = await handler.Handle(CancelCommand(999), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Cancel_ShouldCancelADraft()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host);
        var handler = new CancelStockTransferCommandHandler(host.Context, host.DateTime);

        var result = await handler.Handle(CancelCommand(transfer.Id), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var stored = await ReloadAsync(host);
        Assert.Equal(TransferStatus.Cancelled, stored.Status);
        Assert.Equal("not needed", stored.CancelledReason);
    }

    [Fact]
    public async Task Cancel_ShouldThrowConflict_ForADispatchedTransfer()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host);
        var handler = new CancelStockTransferCommandHandler(host.Context, host.DateTime);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(CancelCommand(transfer.Id), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Cancel_ShouldMapAConcurrencyLoss_ToConflict()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host);
        var handler = new CancelStockTransferCommandHandler(host.Context, host.DateTime);
        host.SaveFaults.Arm(failWithConcurrency: true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(CancelCommand(transfer.Id), TestContext.Current.CancellationToken));

        Assert.Equal(TransferSaving.ConcurrencyMessage, ex.Message);
    }

    [Theory]
    [InlineData(0, "r", false)]
    [InlineData(1, "", false)]
    [InlineData(1, "r", true)]
    public void CancelValidator_ShouldRequireIdAndReason(
        long id,
        string reason,
        bool expected)
    {
        var result = new CancelStockTransferCommandValidator().Validate(CancelCommand(id, reason));

        Assert.Equal(expected, result.IsValid);
    }
}
