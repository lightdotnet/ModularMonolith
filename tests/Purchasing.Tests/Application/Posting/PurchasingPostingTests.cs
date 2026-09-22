using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Purchasing.Tests.TestSupport;
using StarterKit.Inventory.Contracts.Common;
using StarterKit.Inventory.Contracts.Exceptions;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Inventory.Contracts.Stock;
using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Domain.GoodsReceipts;
using StarterKit.Purchasing.Api.Domain.PurchaseReturns;
using ValidationException = Light.Exceptions.ValidationException;

namespace Purchasing.Tests.Application.Posting;

/// <summary>
/// <see cref="PurchasingPosting"/> against a real Sqlite <c>PurchasingDbContext</c>; only the
/// cross-module <see cref="IInventoryService"/> seam is mocked.
/// </summary>
public class PurchasingPostingTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Task<PostingOutcome> ReceiveAsync(
        PurchasingTestHost host,
        Mock<IInventoryService> inventory,
        GoodsReceipt receipt,
        bool landed = false) =>
        PurchasingPosting.ReceiveAsync(
            host.Context,
            inventory.Object,
            receipt,
            host.DateTime,
            landed,
            NullLogger.Instance,
            Ct);

    private static Task<PostingOutcome> PostReturnAsync(
        PurchasingTestHost host,
        Mock<IInventoryService> inventory,
        PurchaseReturn purchaseReturn) =>
        PurchasingPosting.PostReturnAsync(
            host.Context,
            inventory.Object,
            purchaseReturn,
            host.DateTime,
            NullLogger.Instance,
            Ct);

    private static async Task<(GoodsReceipt Receipt, long OrderId)> SeedReceivingAsync(
        PurchasingTestHost host,
        int quantity = 4)
    {
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));
        var receipt = await PurchasingSeed.ReceivingAsync(host, order, null, (order.Lines[0].Id, quantity));

        return (receipt, order.Id);
    }

    private static async Task<(PurchaseReturn Return, long OrderId)> SeedReturnPostingAsync(PurchasingTestHost host)
    {
        var order = await PurchasingSeed.ApprovedAsync(host, (1, 10));
        var receipt = await PurchasingSeed.PostedReceiptAsync(host, order, (order.Lines[0].Id, 6));
        var purchaseReturn = await PurchasingSeed.ReturnPostingAsync(host, receipt, host.DateTime.UtcNow, (receipt.Lines[0].Id, 2));

        return (purchaseReturn, order.Id);
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

    // Issue returns 7 per unit removed.
    private static void SetupIssueValue(Mock<IInventoryService> inventory) =>
        inventory
            .Setup(x => x.IssueStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
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
                    .Select(x => new StockPostingResult(x.ProductId, x.SourceLineId, x.Quantity, 7m, 7m * x.Quantity))
                    .ToList());

    // Goods receipt

    [Fact]
    public async Task ReceiveAsync_ShouldReceiveAtTheFrozenCost_MarkPosted_AndApplyTheQuantitiesToTheOrder()
    {
        // Arrange
        using var host = new PurchasingTestHost();
        var (receipt, orderId) = await SeedReceivingAsync(host);
        var inventory = InventoryMock.Create();

        // Act
        var outcome = await ReceiveAsync(host, inventory, receipt);

        // Assert
        Assert.Equal(PostingOutcomeKind.Completed, outcome.Kind);
        inventory.Verify(
            x => x.ReceiveStockAsync(
                StockSourceType.GoodsReceipt,
                receipt.Id,
                PurchasingBuilder.Location,
                It.Is<IReadOnlyList<StockInLine>>(lines =>
                    lines.Count == 1 && lines[0].Quantity == 4 && lines[0].UnitCostBase == 5m && lines[0].ProductId == 1),
                "receiver",
                It.IsAny<CancellationToken>()),
            Times.Once);

        PurchasingSeed.Detach(host);
        var order = await host.Context.PurchaseOrders.Include(x => x.Lines).SingleAsync(x => x.Id == orderId, Ct);
        Assert.Equal(GoodsReceiptStatus.Posted, await ReceiptStatusAsync(host, receipt.Id));
        Assert.Equal(4, order.TotalReceivedQuantity);
        Assert.Equal(StarterKit.Purchasing.Contracts.Common.PurchaseOrderStatus.PartiallyReceived, order.Status);
    }

    [Fact]
    public async Task ReceiveAsync_ShouldVoidTheReceipt_WithAFixedReason_WhenInventoryRefusesAndNothingLanded()
    {
        // Arrange
        using var host = new PurchasingTestHost();
        var (receipt, orderId) = await SeedReceivingAsync(host);
        var inventory = InventoryMock.Create(anyLanded: false);
        var failure = new ValidationException(new Dictionary<string, string[]> { ["location"] = ["secret detail"] });
        inventory.SetupReceiveThrows(failure);

        // Act
        var outcome = await ReceiveAsync(host, inventory, receipt);

        // Assert
        Assert.Equal(PostingOutcomeKind.Refused, outcome.Kind);
        Assert.Same(failure, outcome.Failure);
        PurchasingSeed.Detach(host);
        var stored = await host.Context.GoodsReceipts.SingleAsync(Ct);
        Assert.Equal(GoodsReceiptStatus.Voided, stored.Status);
        Assert.Equal("Inventory refused the receipt", stored.VoidReason);
        var order = await host.Context.PurchaseOrders.Include(x => x.Lines).SingleAsync(x => x.Id == orderId, Ct);
        Assert.Equal(0, order.TotalReceivedQuantity);
    }

    [Fact]
    public async Task ReceiveAsync_ShouldApplyTheReceipt_WhenValidationFailsButPostingsLanded()
    {
        using var host = new PurchasingTestHost();
        var (receipt, _) = await SeedReceivingAsync(host);
        var inventory = InventoryMock.Create(anyLanded: true);
        inventory.SetupReceiveThrows(new ValidationException(new Dictionary<string, string[]> { ["x"] = ["y"] }));

        var outcome = await ReceiveAsync(host, inventory, receipt);

        Assert.Equal(PostingOutcomeKind.Completed, outcome.Kind);
        Assert.Equal(GoodsReceiptStatus.Posted, await ReceiptStatusAsync(host, receipt.Id));
    }

    [Fact]
    public async Task ReceiveAsync_ShouldPropagateATransientConflict_AndLeaveTheReceiptPosting()
    {
        using var host = new PurchasingTestHost();
        var (receipt, _) = await SeedReceivingAsync(host);
        var inventory = InventoryMock.Create();
        inventory.SetupReceiveThrows(new ConflictException("Stock was modified concurrently"));

        await Assert.ThrowsAsync<ConflictException>(() => ReceiveAsync(host, inventory, receipt));

        Assert.Equal(GoodsReceiptStatus.Posting, await ReceiptStatusAsync(host, receipt.Id));
    }

    [Fact]
    public async Task ReceiveAsync_ShouldSkipInventory_WhenThePostingsAreKnownToHaveLanded()
    {
        using var host = new PurchasingTestHost();
        var (receipt, _) = await SeedReceivingAsync(host);
        var inventory = InventoryMock.Create();

        var outcome = await ReceiveAsync(host, inventory, receipt, landed: true);

        Assert.Equal(PostingOutcomeKind.Completed, outcome.Kind);
        inventory.VerifyReceiveCalls(Times.Never());
    }

    [Fact]
    public async Task ReceiveAsync_ShouldRetryCommitTwo_WhenAConcurrencyLossHappensAfterTheStockLanded()
    {
        using var host = new PurchasingTestHost();
        var (receipt, orderId) = await SeedReceivingAsync(host);
        var inventory = InventoryMock.Create();
        host.SaveFaults.Arm(failTimes: 1);

        var outcome = await ReceiveAsync(host, inventory, receipt);

        Assert.Equal(PostingOutcomeKind.Completed, outcome.Kind);
        inventory.VerifyReceiveCalls(Times.Once());
        Assert.Equal(GoodsReceiptStatus.Posted, await ReceiptStatusAsync(host, receipt.Id));
        var order = await host.Context.PurchaseOrders.Include(x => x.Lines).SingleAsync(x => x.Id == orderId, Ct);
        Assert.Equal(4, order.TotalReceivedQuantity);
    }

    [Fact]
    public async Task ReceiveAsync_ShouldThrowTheStockRecordedConflict_WhenCommitTwoKeepsLosingTheRace()
    {
        using var host = new PurchasingTestHost();
        var (receipt, _) = await SeedReceivingAsync(host);
        var inventory = InventoryMock.Create();
        host.SaveFaults.Arm(failTimes: 3);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => ReceiveAsync(host, inventory, receipt));

        Assert.Equal(PurchasingPosting.StockRecordedMessage, ex.Message);
        Assert.Equal(3, host.SaveFaults.SavesIntercepted);
        inventory.VerifyReceiveCalls(Times.Once());
        Assert.Equal(GoodsReceiptStatus.Posting, await ReceiptStatusAsync(host, receipt.Id));
    }

    // Purchase return

    [Fact]
    public async Task PostReturnAsync_ShouldRecordCostRemovedAndExpectedCredit_AndRegisterTheReturnOnTheOrder()
    {
        // Arrange
        using var host = new PurchasingTestHost();
        var (purchaseReturn, orderId) = await SeedReturnPostingAsync(host);
        var inventory = InventoryMock.Create();
        SetupIssueValue(inventory);

        // Act
        var outcome = await PostReturnAsync(host, inventory, purchaseReturn);

        // Assert
        Assert.Equal(PostingOutcomeKind.Completed, outcome.Kind);
        inventory.Verify(
            x => x.IssueStockAsync(
                StockSourceType.PurchaseReturn,
                purchaseReturn.Id,
                PurchasingBuilder.Location,
                It.IsAny<IReadOnlyList<StockOutLine>>(),
                "poster",
                It.IsAny<CancellationToken>()),
            Times.Once);

        PurchasingSeed.Detach(host);
        var stored = await host.Context.PurchaseReturns.Include(x => x.Lines).SingleAsync(Ct);
        Assert.Equal(PurchaseReturnStatus.Posted, stored.Status);
        Assert.Equal(14m, stored.CostRemovedBase);
        Assert.Equal(10m, stored.ExpectedCreditBase);
        var order = await host.Context.PurchaseOrders.Include(x => x.Lines).SingleAsync(x => x.Id == orderId, Ct);
        Assert.Equal(2, order.Lines[0].ReturnedQuantity);
        Assert.Equal(StarterKit.Purchasing.Contracts.Common.PurchaseOrderStatus.PartiallyReceived, order.Status);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PostReturnAsync_ShouldAbortToDraft_WhenInventoryRefusesAndNothingLanded(bool insufficient)
    {
        using var host = new PurchasingTestHost();
        var (purchaseReturn, _) = await SeedReturnPostingAsync(host);
        var inventory = InventoryMock.Create(anyLanded: false);
        var failure = insufficient
            ? (Exception)new InsufficientStockException("Insufficient stock")
            : new ValidationException(new Dictionary<string, string[]> { ["x"] = ["y"] });
        inventory.SetupIssueThrows(failure);

        var outcome = await PostReturnAsync(host, inventory, purchaseReturn);

        Assert.Equal(PostingOutcomeKind.Refused, outcome.Kind);
        Assert.Same(failure, outcome.Failure);
        Assert.Equal(PurchaseReturnStatus.Draft, await ReturnStatusAsync(host, purchaseReturn.Id));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PostReturnAsync_ShouldRollForward_WhenPostingsLanded_DespiteARefusalStyleFailure(bool insufficient)
    {
        using var host = new PurchasingTestHost();
        var (purchaseReturn, _) = await SeedReturnPostingAsync(host);
        var inventory = InventoryMock.Create(anyLanded: true);
        var failure = insufficient
            ? (Exception)new InsufficientStockException("Insufficient stock")
            : new ValidationException(new Dictionary<string, string[]> { ["x"] = ["y"] });
        inventory
            .SetupSequence(x => x.IssueStockAsync(
                It.IsAny<StockSourceType>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<StockOutLine>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure)
            .ReturnsAsync(purchaseReturn.Lines
                .Select(x => new StockPostingResult(x.ProductId, x.Id, x.Quantity, 3m, 3m * x.Quantity))
                .ToList());

        var outcome = await PostReturnAsync(host, inventory, purchaseReturn);

        Assert.Equal(PostingOutcomeKind.Completed, outcome.Kind);
        Assert.Equal(PurchaseReturnStatus.Posted, await ReturnStatusAsync(host, purchaseReturn.Id));
    }

    [Fact]
    public async Task PostReturnAsync_ShouldStayPosting_WhenTheFailureIsATransientConflict()
    {
        using var host = new PurchasingTestHost();
        var (purchaseReturn, _) = await SeedReturnPostingAsync(host);
        var inventory = InventoryMock.Create(anyLanded: false);
        var failure = new ConflictException("Stock was modified concurrently");
        inventory.SetupIssueThrows(failure);

        var outcome = await PostReturnAsync(host, inventory, purchaseReturn);

        Assert.Equal(PostingOutcomeKind.Pending, outcome.Kind);
        Assert.Same(failure, outcome.Failure);
        Assert.Equal(PurchaseReturnStatus.Posting, await ReturnStatusAsync(host, purchaseReturn.Id));
    }

    [Fact]
    public async Task PostReturnAsync_ShouldRetryCommitTwo_ReusingTheIssuedResults()
    {
        using var host = new PurchasingTestHost();
        var (purchaseReturn, _) = await SeedReturnPostingAsync(host);
        var inventory = InventoryMock.Create();
        SetupIssueValue(inventory);
        host.SaveFaults.Arm(failTimes: 1);

        var outcome = await PostReturnAsync(host, inventory, purchaseReturn);

        Assert.Equal(PostingOutcomeKind.Completed, outcome.Kind);
        inventory.VerifyIssueCalls(Times.Once());
        Assert.Equal(PurchaseReturnStatus.Posted, await ReturnStatusAsync(host, purchaseReturn.Id));
    }

    [Fact]
    public async Task PostReturnAsync_ShouldThrowTheStockRecordedConflict_WhenCommitTwoKeepsLosing()
    {
        using var host = new PurchasingTestHost();
        var (purchaseReturn, _) = await SeedReturnPostingAsync(host);
        var inventory = InventoryMock.Create();
        SetupIssueValue(inventory);
        host.SaveFaults.Arm(failTimes: 3);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => PostReturnAsync(host, inventory, purchaseReturn));

        Assert.Equal(PurchasingPosting.StockRecordedMessage, ex.Message);
        Assert.Equal(PurchaseReturnStatus.Posting, await ReturnStatusAsync(host, purchaseReturn.Id));
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

        Assert.Equal(expected, PurchasingPosting.IsRefusal(exception));
    }
}
