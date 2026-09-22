using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;
using StarterKit.Inventory.Contracts.Exceptions;
using StarterKit.Transfers.Api.Application.StockTransfers;
using StarterKit.Transfers.Api.Application.StockTransfers.Commands;
using StarterKit.Transfers.Contracts.Common;
using Transfers.Tests.TestSupport;

namespace Transfers.Tests.Application.StockTransfers.Commands;

public class DispatchStockTransferCommandHandlerTests
{
    private static Task<TransferStatus> StoredStatusAsync(TransfersTestHost host)
    {
        TransferSeed.Detach(host);

        return host.Context.StockTransfers
            .Select(x => x.Status)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTheTransferDoesNotExist()
    {
        using var host = new TransfersTestHost();
        var handler = new DispatchStockTransferCommandHandler(host.Context, InventoryMock.Create().Object, host.DateTime);

        var result = await handler.Handle(new DispatchStockTransferCommand(999, "u"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldDispatch_AndFreezeCosts_OnSuccess()
    {
        // Arrange
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host, (1, 10), (2, 4));
        var inventory = InventoryMock.Create();
        inventory.SetupIssueAtCost(2.5m);
        var handler = new DispatchStockTransferCommandHandler(host.Context, inventory.Object, host.DateTime);

        // Act
        var result = await handler.Handle(new DispatchStockTransferCommand(transfer.Id, "u"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        TransferSeed.Detach(host);
        var stored = await host.Context.StockTransfers.Include(x => x.Lines).SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(TransferStatus.Dispatched, stored.Status);
        Assert.Equal(host.DateTime.UtcNow, stored.DispatchedAt);
        Assert.All(stored.Lines, x => Assert.Equal(2.5m, x.UnitCostBase));
    }

    [Fact]
    public async Task Handle_ShouldThrowConflict_WhenTheTransferIsNotADraft()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DispatchedAsync(host);
        var inventory = InventoryMock.Create();
        var handler = new DispatchStockTransferCommandHandler(host.Context, inventory.Object, host.DateTime);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new DispatchStockTransferCommand(transfer.Id, "u"), TestContext.Current.CancellationToken));

        inventory.VerifyIssueCalls(Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldThrowValidation_WhenTheDraftHasNoLines()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host);
        transfer.RemoveLine(transfer.Lines[0].Id);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DispatchStockTransferCommandHandler(host.Context, InventoryMock.Create().Object, host.DateTime);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new DispatchStockTransferCommand(transfer.Id, "u"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldRethrowInsufficientStock_AndReturnTheTransferToDraft()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host);
        var inventory = InventoryMock.Create();
        inventory.SetupIssueThrows(new InsufficientStockException("Insufficient stock for product 1"));
        var handler = new DispatchStockTransferCommandHandler(host.Context, inventory.Object, host.DateTime);

        var ex = await Assert.ThrowsAsync<InsufficientStockException>(() =>
            handler.Handle(new DispatchStockTransferCommand(transfer.Id, "u"), TestContext.Current.CancellationToken));

        Assert.Equal("Insufficient stock for product 1", ex.Message);
        Assert.Equal(TransferStatus.Draft, await StoredStatusAsync(host));
    }

    [Fact]
    public async Task Handle_ShouldRethrowATransientConflict_AndLeaveTheTransferPosting()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host);
        var inventory = InventoryMock.Create();
        inventory.SetupIssueThrows(new ConflictException("Stock was modified concurrently"));
        var handler = new DispatchStockTransferCommandHandler(host.Context, inventory.Object, host.DateTime);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new DispatchStockTransferCommand(transfer.Id, "u"), TestContext.Current.CancellationToken));

        Assert.Equal(TransferStatus.Posting, await StoredStatusAsync(host));
    }

    [Fact]
    public async Task Handle_ShouldMapAConcurrencyLossOnTheFirstCommit_ToConflict()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host);
        var inventory = InventoryMock.Create();
        var handler = new DispatchStockTransferCommandHandler(host.Context, inventory.Object, host.DateTime);
        host.SaveFaults.Arm(failWithConcurrency: true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new DispatchStockTransferCommand(transfer.Id, "u"), TestContext.Current.CancellationToken));

        Assert.Equal(TransferSaving.ConcurrencyMessage, ex.Message);
        inventory.VerifyIssueCalls(Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldMapAConcurrencyLossOnTheSecondCommit_ToConflict()
    {
        using var host = new TransfersTestHost();
        var transfer = await TransferSeed.DraftAsync(host);
        var inventory = InventoryMock.Create();
        inventory.SetupIssueAtCost(1m);
        var handler = new DispatchStockTransferCommandHandler(host.Context, inventory.Object, host.DateTime);
        host.SaveFaults.Arm(skipSaves: 1, failWithConcurrency: true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new DispatchStockTransferCommand(transfer.Id, "u"), TestContext.Current.CancellationToken));

        Assert.Equal(TransferSaving.ConcurrencyMessage, ex.Message);
        // Stock already left the source; the transfer stays Posting for the reconciliation sweep.
        Assert.Equal(TransferStatus.Posting, await StoredStatusAsync(host));
    }

    [Theory]
    [InlineData(0, "u", false)]
    [InlineData(1, "", false)]
    [InlineData(1, "u", true)]
    public void Validator_ShouldRequireAPositiveIdAndUser(
        long id,
        string user,
        bool expected)
    {
        var result = new DispatchStockTransferCommandValidator().Validate(new DispatchStockTransferCommand(id, user));

        Assert.Equal(expected, result.IsValid);
    }
}
