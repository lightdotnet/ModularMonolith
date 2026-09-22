using Light.Exceptions;
using Purchasing.Tests.TestSupport;
using StarterKit.Purchasing.Api.Domain.GoodsReceipts;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using StarterKit.Purchasing.Api.Domain.PurchaseReturns;
using StarterKit.Purchasing.Contracts.Common;
using ValidationException = Light.Exceptions.ValidationException;

namespace Purchasing.Tests.Domain;

public class GoodsReceiptAndReturnTests
{
    private static readonly DateTimeOffset Now = PurchasingBuilder.Now;

    // Approved order: line 1 (10 @ 5), line 2 (4 @ 5).
    private static PurchaseOrder Order() => PurchasingBuilder.Approved((1, 10), (2, 4));

    private static PurchaseReturnReason? NoReason => null;

    private static IReadOnlyCollection<(long, int, PurchaseReturnReason?)> ReturnLines(params (long LineId, int Quantity)[] lines) =>
        lines.Select(x => (x.LineId, x.Quantity, NoReason)).ToList();

    // GoodsReceipt

    [Fact]
    public void Receipt_Create_ShouldSnapshotTheOrderLines_AndStartPosting()
    {
        var order = Order();

        var receipt = GoodsReceipt.Create(
            order,
            "  DN-1 ",
            Now.AddMinutes(2),
            "receiver",
            [(1, 4), (2, 1)],
            new Dictionary<long, int>(),
            Now.AddMinutes(2));

        Assert.Equal(GoodsReceiptStatus.Posting, receipt.Status);
        Assert.Equal("DN-1", receipt.DeliveryNoteRef);
        Assert.Equal("receiver", receipt.ReceivedByUserId);
        Assert.Equal(order.LocationId, receipt.LocationId);
        Assert.Equal(5, receipt.TotalQuantity);
        Assert.Equal(25m, receipt.TotalCostBase);
        Assert.All(receipt.Lines, x => Assert.Equal(5m, x.UnitCostBase));
        // Receiving does not touch the order until the posting is confirmed.
        Assert.Equal(0, order.TotalReceivedQuantity);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Receipt_Create_ShouldStoreABlankDeliveryNoteAsNull(string? note)
    {
        var receipt = GoodsReceipt.Create(Order(), note, Now.AddMinutes(2), "u", [(1, 1)], new Dictionary<long, int>(), Now);

        Assert.Null(receipt.DeliveryNoteRef);
    }

    [Fact]
    public void Receipt_Create_ShouldRejectOverReceipt_IncludingInFlightQuantity()
    {
        var order = Order();

        Assert.Throws<ValidationException>(() =>
            GoodsReceipt.Create(order, null, Now.AddMinutes(2), "u", [(1, 11)], new Dictionary<long, int>(), Now));
        Assert.Throws<ValidationException>(() =>
            GoodsReceipt.Create(order, null, Now.AddMinutes(2), "u", [(1, 3)], new Dictionary<long, int> { [1] = 8 }, Now));
    }

    [Fact]
    public void Receipt_Create_ShouldRejectAReceivedDateBeforeApproval_AndAnUnreceivableOrder()
    {
        var order = Order();

        Assert.Throws<ValidationException>(() =>
            GoodsReceipt.Create(order, null, order.ApprovedAt!.Value.AddMinutes(-1), "u", [(1, 1)], new Dictionary<long, int>(), Now));
        Assert.Throws<ConflictException>(() =>
            GoodsReceipt.Create(PurchasingBuilder.DraftWithLines(), null, Now, "u", [(1, 1)], new Dictionary<long, int>(), Now));
        Assert.Throws<ArgumentNullException>(() =>
            GoodsReceipt.Create(null!, null, Now, "u", [(1, 1)], new Dictionary<long, int>(), Now));
    }

    [Fact]
    public void Receipt_MarkPosted_ShouldStampOnce_AndBeANoOpAfterwards()
    {
        var receipt = PurchasingBuilder.Receipt(Order(), 5, (1, 2));

        receipt.MarkPosted(Now.AddMinutes(5));
        receipt.MarkPosted(Now.AddMinutes(9));

        Assert.Equal(GoodsReceiptStatus.Posted, receipt.Status);
        Assert.Equal(Now.AddMinutes(5), receipt.StockPostedAt);
    }

    [Fact]
    public void Receipt_Void_ShouldOnlyWorkWhilePosting()
    {
        var receipt = PurchasingBuilder.Receipt(Order(), 5, (1, 2));

        receipt.Void("refused", Now.AddMinutes(4));

        Assert.Equal(GoodsReceiptStatus.Voided, receipt.Status);
        Assert.Equal("refused", receipt.VoidReason);
        Assert.Equal(Now.AddMinutes(4), receipt.VoidedAt);
        Assert.Throws<ConflictException>(() => receipt.Void("again", Now));

        var posted = PurchasingBuilder.PostedReceipt(Order(), 6, (1, 2));
        Assert.Throws<ConflictException>(() => posted.Void("x", Now));
    }

    [Fact]
    public void Receipt_EnsureReturnable_ShouldRequirePosted()
    {
        Assert.Throws<ConflictException>(() => PurchasingBuilder.Receipt(Order(), 5, (1, 2)).EnsureReturnable());
        PurchasingBuilder.PostedReceipt(Order(), 6, (1, 2)).EnsureReturnable();
    }

    [Fact]
    public void Receipt_FindLine_ShouldRejectAnUnknownLine()
    {
        var receipt = PurchasingBuilder.PostedReceipt(Order(), 6, (1, 2));

        Assert.Same(receipt.Lines[0], receipt.FindLine(receipt.Lines[0].Id));
        Assert.Throws<ValidationException>(() => receipt.FindLine(99999));
    }

    // PurchaseReturn

    [Fact]
    public void Return_Create_ShouldSnapshotTheReceipt_AndStartAsDraft()
    {
        var receipt = PurchasingBuilder.PostedReceipt(Order(), 6, (1, 6), (2, 4));

        var purchaseReturn = PurchaseReturn.Create(
            receipt,
            PurchaseReturnReason.Damaged,
            "note",
            ReturnLines((receipt.Lines[0].Id, 2)),
            new Dictionary<long, int>(),
            Now);

        Assert.Equal(PurchaseReturnStatus.Draft, purchaseReturn.Status);
        Assert.Equal(receipt.LocationId, purchaseReturn.LocationId);
        Assert.Equal(receipt.SupplierId, purchaseReturn.SupplierId);
        Assert.Equal(2, purchaseReturn.TotalQuantity);
        Assert.Equal(5m, purchaseReturn.Lines[0].ReceiptUnitCostBase);
        Assert.Null(purchaseReturn.CostRemovedBase);
    }

    [Fact]
    public void Return_Create_ShouldRequireAPostedReceipt()
    {
        var receipt = PurchasingBuilder.Receipt(Order(), 6, (1, 6));

        Assert.Throws<ConflictException>(() => PurchaseReturn.Create(
            receipt,
            PurchaseReturnReason.Other,
            null,
            ReturnLines((receipt.Lines[0].Id, 1)),
            new Dictionary<long, int>(),
            Now));
    }

    [Fact]
    public void Return_Create_ShouldEnforceTheOverReturnGuard_CountingWhatOtherReturnsClaim()
    {
        var receipt = PurchasingBuilder.PostedReceipt(Order(), 6, (1, 6));
        var lineId = receipt.Lines[0].Id;

        Assert.Throws<ValidationException>(() => PurchaseReturn.Create(
            receipt, PurchaseReturnReason.Other, null, ReturnLines((lineId, 7)), new Dictionary<long, int>(), Now));
        Assert.Throws<ValidationException>(() => PurchaseReturn.Create(
            receipt, PurchaseReturnReason.Other, null, ReturnLines((lineId, 3)), new Dictionary<long, int> { [lineId] = 4 }, Now));
        Assert.NotNull(PurchaseReturn.Create(
            receipt, PurchaseReturnReason.Other, null, ReturnLines((lineId, 2)), new Dictionary<long, int> { [lineId] = 4 }, Now));
    }

    [Fact]
    public void Return_Create_ShouldRejectEmptyDuplicateNonPositiveAndUnknownLines()
    {
        var receipt = PurchasingBuilder.PostedReceipt(Order(), 6, (1, 6));
        var lineId = receipt.Lines[0].Id;
        var none = new Dictionary<long, int>();

        Assert.Throws<ValidationException>(() => PurchaseReturn.Create(receipt, PurchaseReturnReason.Other, null, ReturnLines(), none, Now));
        Assert.Throws<ValidationException>(() => PurchaseReturn.Create(receipt, PurchaseReturnReason.Other, null, ReturnLines((lineId, 1), (lineId, 1)), none, Now));
        Assert.Throws<ValidationException>(() => PurchaseReturn.Create(receipt, PurchaseReturnReason.Other, null, ReturnLines((lineId, 0)), none, Now));
        Assert.Throws<ValidationException>(() => PurchaseReturn.Create(receipt, PurchaseReturnReason.Other, null, ReturnLines((99999, 1)), none, Now));
    }

    [Fact]
    public void Return_UpdateDraft_ShouldReplaceLines_ExcludingItsOwnClaimFromTheGuard()
    {
        var receipt = PurchasingBuilder.PostedReceipt(Order(), 6, (1, 6));
        var lineId = receipt.Lines[0].Id;
        var purchaseReturn = PurchasingBuilder.Return(receipt, 3, (lineId, 5));

        // The caller passes what OTHER returns claim (0 here), so the edited return may go up to 6.
        purchaseReturn.UpdateDraft(receipt, PurchaseReturnReason.WrongItem, "n", ReturnLines((lineId, 6)), new Dictionary<long, int>());

        Assert.Equal(6, purchaseReturn.TotalQuantity);
        Assert.Equal(PurchaseReturnReason.WrongItem, purchaseReturn.Reason);
        Assert.Throws<ValidationException>(() => purchaseReturn.UpdateDraft(
            receipt, PurchaseReturnReason.WrongItem, null, ReturnLines((lineId, 6)), new Dictionary<long, int> { [lineId] = 1 }));
        // A failed replace leaves the previous lines intact.
        Assert.Equal(6, purchaseReturn.TotalQuantity);
    }

    [Fact]
    public void Return_UpdateDraft_ShouldRejectAForeignReceipt_AndANonDraft()
    {
        var receipt = PurchasingBuilder.PostedReceipt(Order(), 6, (1, 6));
        var other = PurchasingBuilder.PostedReceipt(Order(), 7, (1, 6));
        var purchaseReturn = PurchasingBuilder.Return(receipt, 3, (receipt.Lines[0].Id, 1));

        Assert.Throws<ValidationException>(() => purchaseReturn.UpdateDraft(
            other, PurchaseReturnReason.Other, null, ReturnLines((other.Lines[0].Id, 1)), new Dictionary<long, int>()));

        purchaseReturn.BeginPost("u", Now);
        Assert.Throws<ConflictException>(() => purchaseReturn.UpdateDraft(
            receipt, PurchaseReturnReason.Other, null, ReturnLines((receipt.Lines[0].Id, 1)), new Dictionary<long, int>()));
    }

    [Fact]
    public void Return_ShouldFollowDraftPostingPostedCredited_RecordingCostsAndExpectedCredit()
    {
        var receipt = PurchasingBuilder.PostedReceipt(Order(), 6, (1, 6), (2, 4));
        var purchaseReturn = PurchasingBuilder.Return(receipt, 3, (receipt.Lines[0].Id, 2), (receipt.Lines[1].Id, 1));

        purchaseReturn.BeginPost("poster", Now.AddMinutes(1));

        Assert.Equal(PurchaseReturnStatus.Posting, purchaseReturn.Status);
        Assert.Equal(Now.AddMinutes(1), purchaseReturn.PostingStartedAt);
        Assert.Equal("poster", purchaseReturn.PostedBy);
        Assert.Null(purchaseReturn.CostRemovedBase);

        purchaseReturn.CompletePost(
            new Dictionary<long, decimal> { [purchaseReturn.Lines[0].Id] = 9m, [purchaseReturn.Lines[1].Id] = 4m },
            Now.AddMinutes(2));

        Assert.Equal(PurchaseReturnStatus.Posted, purchaseReturn.Status);
        Assert.Equal(13m, purchaseReturn.CostRemovedBase);
        Assert.Equal(15m, purchaseReturn.ExpectedCreditBase);
        Assert.Null(purchaseReturn.PostingStartedAt);
        Assert.Equal(Now.AddMinutes(2), purchaseReturn.PostedAt);

        purchaseReturn.MarkCredited("CN-1", 14m, "acct", Now.AddDays(1));

        Assert.Equal(PurchaseReturnStatus.Credited, purchaseReturn.Status);
        Assert.Equal("CN-1", purchaseReturn.CreditNoteNumber);
        Assert.Equal(14m, purchaseReturn.CreditAmountBase);
        Assert.Equal("acct", purchaseReturn.CreditedBy);
        Assert.Equal(13m, purchaseReturn.CostRemovedBase);
        Assert.Throws<ConflictException>(() => purchaseReturn.MarkCredited("CN-2", 1m, "u", Now));
    }

    [Fact]
    public void Return_CompletePost_ShouldRequireACostForEveryLine()
    {
        var receipt = PurchasingBuilder.PostedReceipt(Order(), 6, (1, 6), (2, 4));
        var purchaseReturn = PurchasingBuilder.Return(receipt, 3, (receipt.Lines[0].Id, 2), (receipt.Lines[1].Id, 1));
        purchaseReturn.BeginPost("u", Now);

        Assert.Throws<ValidationException>(() =>
            purchaseReturn.CompletePost(new Dictionary<long, decimal> { [purchaseReturn.Lines[0].Id] = 1m }, Now));
    }

    [Fact]
    public void Return_AbortPost_ShouldReturnToDraft_OnlyFromPosting()
    {
        var receipt = PurchasingBuilder.PostedReceipt(Order(), 6, (1, 6));
        var purchaseReturn = PurchasingBuilder.Return(receipt, 3, (receipt.Lines[0].Id, 2));

        Assert.Throws<ConflictException>(() => purchaseReturn.AbortPost());

        purchaseReturn.BeginPost("u", Now);
        purchaseReturn.AbortPost();

        Assert.Equal(PurchaseReturnStatus.Draft, purchaseReturn.Status);
        Assert.Null(purchaseReturn.PostingStartedAt);
    }

    [Fact]
    public void Return_BeginPost_ShouldConflictOutsideDraft_AndCompletePostOutsidePosting()
    {
        var receipt = PurchasingBuilder.PostedReceipt(Order(), 6, (1, 6));
        var purchaseReturn = PurchasingBuilder.Return(receipt, 3, (receipt.Lines[0].Id, 2));

        Assert.Throws<ConflictException>(() => purchaseReturn.CompletePost(new Dictionary<long, decimal>(), Now));
        Assert.Throws<ConflictException>(() => purchaseReturn.MarkCredited("CN", 1m, "u", Now));

        purchaseReturn.BeginPost("u", Now);
        Assert.Throws<ConflictException>(() => purchaseReturn.BeginPost("u", Now));
        Assert.Throws<ConflictException>(() => purchaseReturn.Cancel("r", "u", Now));
    }

    [Fact]
    public void Return_Cancel_ShouldWorkOnlyFromDraft_AndRecordTheActor()
    {
        var receipt = PurchasingBuilder.PostedReceipt(Order(), 6, (1, 6));
        var purchaseReturn = PurchasingBuilder.Return(receipt, 3, (receipt.Lines[0].Id, 2));

        purchaseReturn.Cancel("mistake", "u1", Now.AddMinutes(1));

        Assert.Equal(PurchaseReturnStatus.Cancelled, purchaseReturn.Status);
        Assert.Equal("u1", purchaseReturn.CancelledBy);
        Assert.Equal("mistake", purchaseReturn.CancelledReason);
        Assert.Throws<ConflictException>(() => purchaseReturn.Cancel("again", "u", Now));

        var posted = PurchasingBuilder.Return(receipt, 4, (receipt.Lines[0].Id, 1));
        posted.BeginPost("u", Now);
        posted.CompletePost(new Dictionary<long, decimal> { [posted.Lines[0].Id] = 1m }, Now);
        Assert.Throws<ConflictException>(() => posted.Cancel("no", "u", Now));
    }
}
