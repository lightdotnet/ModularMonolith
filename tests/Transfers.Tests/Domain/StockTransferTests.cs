using Light.Exceptions;
using StarterKit.Transfers.Api.Domain.StockTransfers;
using StarterKit.Transfers.Contracts.Common;
using Transfers.Tests.TestSupport;

namespace Transfers.Tests.Domain;

public class StockTransferTests
{
    private static readonly DateTimeOffset Now = TransferBuilder.Now;

    // Drives the real state machine into the requested status; every transfer has two lines (ids 1, 2)
    // of quantity 10 and 4, dispatched at a unit cost of 5.
    private static StockTransfer InState(TransferStatus status)
    {
        switch (status)
        {
            case TransferStatus.Draft:
                return TransferBuilder.DraftWithLines((1, 10), (2, 4));

            case TransferStatus.Posting:
                return TransferBuilder.Posting((1, 10), (2, 4));

            case TransferStatus.Dispatched:
                return TransferBuilder.Dispatched((1, 10), (2, 4));

            case TransferStatus.PartiallyReceived:
            {
                var transfer = TransferBuilder.Dispatched((1, 10), (2, 4));
                TransferBuilder.PostedReceipt(transfer, "r-partial", 900, (1, 3));
                return transfer;
            }

            case TransferStatus.Received:
            {
                var transfer = TransferBuilder.Dispatched((1, 10), (2, 4));
                TransferBuilder.PostedReceipt(transfer, "r-all", 900, (1, 10), (2, 4));
                return transfer;
            }

            case TransferStatus.Closed:
            {
                var transfer = TransferBuilder.Dispatched((1, 10), (2, 4));
                transfer.Close("lost", Now);
                return transfer;
            }

            case TransferStatus.Cancelled:
            {
                var transfer = TransferBuilder.DraftWithLines((1, 10), (2, 4));
                transfer.Cancel("nope", Now);
                return transfer;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
    }

    public static TheoryData<TransferStatus> NonDraftStatuses() =>
        new(TransferBuilder.StatusesOtherThan(TransferStatus.Draft));

    public static TheoryData<TransferStatus> StatusesThatCannotReceive() =>
        new(TransferBuilder.StatusesOtherThan(TransferStatus.Dispatched, TransferStatus.PartiallyReceived));

    public static TheoryData<TransferStatus> StatusesThatCannotClose() =>
        new(TransferBuilder.StatusesOtherThan(TransferStatus.Dispatched, TransferStatus.PartiallyReceived));

    public static TheoryData<TransferStatus> StatusesOtherThanPosting() =>
        new(TransferBuilder.StatusesOtherThan(TransferStatus.Posting));

    // Create / header

    [Fact]
    public void Create_ShouldStartAsDraft_WithGeneratedCodeAndTrimmedIds()
    {
        var transfer = StockTransfer.Create("  a  ", "A", " b ", "B", "note", Now);

        Assert.Equal(TransferStatus.Draft, transfer.Status);
        Assert.Equal("a", transfer.SourceLocationId);
        Assert.Equal("b", transfer.DestinationLocationId);
        Assert.Equal(Now, transfer.RequestedAt);
        Assert.StartsWith("T20260101", transfer.TransferCode.Value);
        Assert.Empty(transfer.Lines);
    }

    [Theory]
    [InlineData("a", "a")]
    [InlineData(" a ", "a")]
    [InlineData("LOC-1", "loc-1")]
    public void Create_ShouldReject_WhenSourceEqualsDestination_TrimmedAndCaseInsensitive(
        string source,
        string destination)
    {
        Assert.Throws<ValidationException>(() => StockTransfer.Create(source, "S", destination, "D", null, Now));
    }

    [Fact]
    public void UpdateHeader_ShouldApply_WhenDraft()
    {
        var transfer = TransferBuilder.Draft();

        transfer.UpdateHeader("x", "X", "y", "Y", "n");

        Assert.Equal("x", transfer.SourceLocationId);
        Assert.Equal("Y", transfer.DestinationLocationName);
        Assert.Equal("n", transfer.Note);
    }

    [Fact]
    public void UpdateHeader_ShouldReject_WhenSourceEqualsDestination()
    {
        var transfer = TransferBuilder.Draft();

        Assert.Throws<ValidationException>(() => transfer.UpdateHeader("x", "X", " X ", "X", null));
    }

    [Theory]
    [MemberData(nameof(NonDraftStatuses))]
    public void UpdateHeader_ShouldConflict_FromAnyNonDraftState(TransferStatus status)
    {
        var transfer = InState(status);

        Assert.Throws<ConflictException>(() => transfer.UpdateHeader("x", "X", "y", "Y", null));
    }

    // Lines

    [Fact]
    public void AddLine_ShouldAddALine_WhenDraft()
    {
        var transfer = TransferBuilder.Draft();

        transfer.AddLine(7, "Widget", "W-7", 3);

        var line = Assert.Single(transfer.Lines);
        Assert.Equal(7, line.ProductId);
        Assert.Equal(3, line.RequestedQuantity);
        Assert.Equal(3, transfer.TotalRequestedQuantity);
    }

    [Fact]
    public void AddLine_ShouldReject_ADuplicateProduct()
    {
        var transfer = TransferBuilder.Draft();
        transfer.AddLine(7, "Widget", "W-7", 3);

        Assert.Throws<ValidationException>(() => transfer.AddLine(7, "Widget", "W-7", 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddLine_ShouldReject_ANonPositiveQuantity(int quantity)
    {
        var transfer = TransferBuilder.Draft();

        Assert.Throws<ValidationException>(() => transfer.AddLine(1, "W", "S", quantity));
        Assert.Empty(transfer.Lines);
    }

    [Theory]
    [MemberData(nameof(NonDraftStatuses))]
    public void AddLine_ShouldConflict_FromAnyNonDraftState(TransferStatus status)
    {
        var transfer = InState(status);

        Assert.Throws<ConflictException>(() => transfer.AddLine(99, "W", "S", 1));
    }

    [Fact]
    public void UpdateLine_ShouldChangeTheQuantity_WhenDraft()
    {
        var transfer = InState(TransferStatus.Draft);

        transfer.UpdateLine(1, 25);

        Assert.Equal(25, transfer.Lines.Single(x => x.Id == 1).RequestedQuantity);
    }

    [Fact]
    public void UpdateLine_ShouldReject_ANonPositiveQuantity_AndAnUnknownLine()
    {
        var transfer = InState(TransferStatus.Draft);

        Assert.Throws<ValidationException>(() => transfer.UpdateLine(1, 0));
        Assert.Throws<ValidationException>(() => transfer.UpdateLine(999, 1));
    }

    [Theory]
    [MemberData(nameof(NonDraftStatuses))]
    public void UpdateLine_ShouldConflict_FromAnyNonDraftState(TransferStatus status)
    {
        var transfer = InState(status);

        Assert.Throws<ConflictException>(() => transfer.UpdateLine(1, 2));
    }

    [Fact]
    public void RemoveLine_ShouldRemove_WhenDraft_AndRejectAnUnknownLine()
    {
        var transfer = InState(TransferStatus.Draft);

        transfer.RemoveLine(1);

        Assert.Single(transfer.Lines);
        Assert.Throws<ValidationException>(() => transfer.RemoveLine(1));
    }

    [Theory]
    [MemberData(nameof(NonDraftStatuses))]
    public void RemoveLine_ShouldConflict_FromAnyNonDraftState(TransferStatus status)
    {
        var transfer = InState(status);

        Assert.Throws<ConflictException>(() => transfer.RemoveLine(1));
    }

    // Dispatch

    [Fact]
    public void BeginDispatch_ShouldMoveToPosting_AndStampPostingStartedAt()
    {
        var transfer = InState(TransferStatus.Draft);

        transfer.BeginDispatch(Now);

        Assert.Equal(TransferStatus.Posting, transfer.Status);
        Assert.Equal(Now, transfer.PostingStartedAt);
    }

    [Fact]
    public void BeginDispatch_ShouldReject_ADraftWithNoLines()
    {
        var transfer = TransferBuilder.Draft();

        Assert.Throws<ValidationException>(() => transfer.BeginDispatch(Now));
        Assert.Equal(TransferStatus.Draft, transfer.Status);
    }

    [Theory]
    [MemberData(nameof(NonDraftStatuses))]
    public void BeginDispatch_ShouldConflict_FromAnyNonDraftState(TransferStatus status)
    {
        var transfer = InState(status);

        Assert.Throws<ConflictException>(() => transfer.BeginDispatch(Now));
    }

    [Fact]
    public void CompleteDispatch_ShouldFreezeQuantitiesAndCosts_AndRaiseAnEvent()
    {
        var transfer = InState(TransferStatus.Posting);

        transfer.CompleteDispatch(
            new Dictionary<long, decimal> { [1] = 2.5m, [2] = 4m },
            Now.AddMinutes(1));

        Assert.Equal(TransferStatus.Dispatched, transfer.Status);
        Assert.Equal(Now.AddMinutes(1), transfer.DispatchedAt);
        Assert.Null(transfer.PostingStartedAt);
        Assert.Equal(10, transfer.Lines.Single(x => x.Id == 1).QtyDispatched);
        Assert.Equal(2.5m, transfer.Lines.Single(x => x.Id == 1).UnitCostBase);
        Assert.Equal(14, transfer.TotalInTransitQuantity);
        Assert.Single(transfer.DomainEvents.OfType<TransferDispatchedEvent>());
    }

    [Fact]
    public void CompleteDispatch_ShouldReject_WhenACostIsMissingForALine()
    {
        var transfer = InState(TransferStatus.Posting);

        Assert.Throws<ValidationException>(() => transfer.CompleteDispatch(
            new Dictionary<long, decimal> { [1] = 1m },
            Now));
    }

    [Theory]
    [MemberData(nameof(StatusesOtherThanPosting))]
    public void CompleteDispatch_And_AbortDispatch_ShouldConflict_UnlessPosting(TransferStatus status)
    {
        var transfer = InState(status);

        Assert.Throws<ConflictException>(() => transfer.CompleteDispatch(
            new Dictionary<long, decimal> { [1] = 1m, [2] = 1m },
            Now));
        Assert.Throws<ConflictException>(() => transfer.AbortDispatch());
    }

    [Fact]
    public void AbortDispatch_ShouldReturnToDraft_AndClearPostingStartedAt()
    {
        var transfer = InState(TransferStatus.Posting);

        transfer.AbortDispatch();

        Assert.Equal(TransferStatus.Draft, transfer.Status);
        Assert.Null(transfer.PostingStartedAt);
    }

    // BeginReceive

    [Fact]
    public void BeginReceive_ShouldRecordAPostingReceipt_WithoutChangingLineQuantities()
    {
        var transfer = InState(TransferStatus.Dispatched);

        var receipt = transfer.BeginReceive("req-1", [(1, 4)], Now);

        Assert.Equal(TransferReceiptStatus.Posting, receipt.Status);
        Assert.Equal("req-1", receipt.ClientRequestId);
        Assert.Equal(4, Assert.Single(receipt.Lines).Quantity);
        Assert.Equal(0, transfer.Lines.Single(x => x.Id == 1).QtyReceived);
        Assert.Equal(TransferStatus.Dispatched, transfer.Status);
    }

    [Theory]
    [MemberData(nameof(StatusesThatCannotReceive))]
    public void BeginReceive_ShouldConflict_FromAnyStateOtherThanDispatchedOrPartiallyReceived(TransferStatus status)
    {
        var transfer = InState(status);

        Assert.Throws<ConflictException>(() => transfer.BeginReceive("fresh", [(1, 1)], Now));
    }

    [Fact]
    public void BeginReceive_ShouldReject_NoLines_DuplicateLines_UnknownLine_AndNonPositiveQuantity()
    {
        var transfer = InState(TransferStatus.Dispatched);

        Assert.Throws<ValidationException>(() => transfer.BeginReceive("a", [], Now));
        Assert.Throws<ValidationException>(() => transfer.BeginReceive("b", [(1, 1), (1, 2)], Now));
        Assert.Throws<ValidationException>(() => transfer.BeginReceive("c", [(999, 1)], Now));
        Assert.Throws<ValidationException>(() => transfer.BeginReceive("d", [(1, 0)], Now));
        Assert.Empty(transfer.Receipts);
    }

    [Fact]
    public void BeginReceive_ShouldReject_OverReceiptBeyondWhatIsInTransit()
    {
        var transfer = InState(TransferStatus.Dispatched);

        Assert.Throws<ValidationException>(() => transfer.BeginReceive("over", [(2, 5)], Now));
        Assert.NotNull(transfer.BeginReceive("exact", [(2, 4)], Now));
    }

    [Fact]
    public void BeginReceive_ShouldSubtractAlreadyReceivedQuantity_FromWhatIsReceivable()
    {
        var transfer = InState(TransferStatus.PartiallyReceived);

        // Line 1: 10 dispatched, 3 received -> 7 left.
        Assert.Throws<ValidationException>(() => transfer.BeginReceive("over", [(1, 8)], Now));
        Assert.NotNull(transfer.BeginReceive("exact", [(1, 7)], Now));
    }

    [Fact]
    public void BeginReceive_ShouldCountPendingPostingReceipts_TowardTheInTransitLimit()
    {
        var transfer = InState(TransferStatus.Dispatched);
        TransferBuilder.BeginReceipt(transfer, "first", 900, (1, 8));

        // 10 dispatched - 8 already pending = 2 available.
        Assert.Throws<ValidationException>(() => transfer.BeginReceive("second", [(1, 3)], Now));
        Assert.NotNull(transfer.BeginReceive("second-ok", [(1, 2)], Now));
    }

    [Fact]
    public void BeginReceive_ShouldNotCountVoidedReceipts()
    {
        var transfer = InState(TransferStatus.Dispatched);
        var voided = TransferBuilder.BeginReceipt(transfer, "first", 900, (1, 10));
        transfer.VoidReceipt(voided.Id, "refused", Now);

        var again = transfer.BeginReceive("second", [(1, 10)], Now);

        Assert.Equal(TransferReceiptStatus.Posting, again.Status);
    }

    [Theory]
    [InlineData("req-1", "req-1")]
    [InlineData("req-1", "  req-1  ")]
    [InlineData("REQ-1", "req-1")]
    public void BeginReceive_ShouldBeIdempotent_ByTrimmedCaseInsensitiveClientRequestId(
        string first,
        string second)
    {
        var transfer = InState(TransferStatus.Dispatched);
        var original = transfer.BeginReceive(first, [(1, 1)], Now);

        // The replay ignores its (different) lines: the recorded receipt is returned as-is.
        var replay = transfer.BeginReceive(second, [(1, 9)], Now);

        Assert.Same(original, replay);
        Assert.Single(transfer.Receipts);
    }

    [Fact]
    public void BeginReceive_ShouldReturnTheExistingReceipt_EvenAfterTheTransferMovedOnToReceived()
    {
        var transfer = InState(TransferStatus.Dispatched);
        var posted = TransferBuilder.PostedReceipt(transfer, "req-1", 900, (1, 10), (2, 4));

        var replay = transfer.BeginReceive("req-1", [(1, 1)], Now);

        Assert.Same(posted, replay);
    }

    // CompleteReceive / VoidReceipt

    [Fact]
    public void CompleteReceive_ShouldApplyQuantities_AndBecomePartiallyReceived()
    {
        var transfer = InState(TransferStatus.Dispatched);
        var receipt = TransferBuilder.BeginReceipt(transfer, "r", 900, (1, 3));

        transfer.CompleteReceive(receipt.Id, Now);

        Assert.Equal(TransferStatus.PartiallyReceived, transfer.Status);
        Assert.Equal(TransferReceiptStatus.Posted, receipt.Status);
        Assert.Equal(3, transfer.Lines.Single(x => x.Id == 1).QtyReceived);
        Assert.Equal(7, transfer.Lines.Single(x => x.Id == 1).QtyInTransit);
        Assert.Null(transfer.ReceivedAt);
        Assert.False(transfer.DomainEvents.OfType<TransferReceivedEvent>().Single().FullyReceived);
    }

    [Fact]
    public void CompleteReceive_ShouldBecomeReceived_WhenEveryLineIsFullyReceived()
    {
        var transfer = InState(TransferStatus.PartiallyReceived);
        var receipt = TransferBuilder.BeginReceipt(transfer, "r2", 901, (1, 7), (2, 4));

        transfer.CompleteReceive(receipt.Id, Now.AddMinutes(2));

        Assert.Equal(TransferStatus.Received, transfer.Status);
        Assert.Equal(Now.AddMinutes(2), transfer.ReceivedAt);
        Assert.Equal(0, transfer.TotalInTransitQuantity);
        Assert.True(transfer.DomainEvents.OfType<TransferReceivedEvent>().Last().FullyReceived);
    }

    [Fact]
    public void CompleteReceive_ShouldBeANoOp_ForAnAlreadyPostedReceipt()
    {
        var transfer = InState(TransferStatus.Dispatched);
        var receipt = TransferBuilder.PostedReceipt(transfer, "r", 900, (1, 3));

        transfer.CompleteReceive(receipt.Id, Now);

        Assert.Equal(3, transfer.Lines.Single(x => x.Id == 1).QtyReceived);
        Assert.Single(transfer.DomainEvents.OfType<TransferReceivedEvent>());
    }

    [Fact]
    public void CompleteReceive_ShouldReject_AnUnknownReceipt()
    {
        var transfer = InState(TransferStatus.Dispatched);

        Assert.Throws<ValidationException>(() => transfer.CompleteReceive(12345, Now));
    }

    [Fact]
    public void CompleteReceive_ShouldConflict_ForANonPostedReceiptOfANoLongerReceivableTransfer()
    {
        var transfer = InState(TransferStatus.Dispatched);
        var receipt = TransferBuilder.BeginReceipt(transfer, "r", 900, (1, 3));
        transfer.VoidReceipt(receipt.Id, "x", Now);
        // A voided receipt does not block Close, which makes the transfer non-receivable.
        transfer.Close("lost", Now);

        Assert.Throws<ConflictException>(() => transfer.CompleteReceive(receipt.Id, Now));
    }

    [Fact]
    public void VoidReceipt_ShouldMarkTheReceiptVoided_WithReasonAndTimestamp()
    {
        var transfer = InState(TransferStatus.Dispatched);
        var receipt = TransferBuilder.BeginReceipt(transfer, "r", 900, (1, 3));

        transfer.VoidReceipt(receipt.Id, "refused", Now.AddMinutes(1));

        Assert.Equal(TransferReceiptStatus.Voided, receipt.Status);
        Assert.Equal("refused", receipt.VoidReason);
        Assert.Equal(Now.AddMinutes(1), receipt.VoidedAt);
        Assert.Equal(0, transfer.Lines.Single(x => x.Id == 1).QtyReceived);
    }

    [Fact]
    public void VoidReceipt_ShouldConflict_ForAPostedOrAlreadyVoidedReceipt()
    {
        var transfer = InState(TransferStatus.Dispatched);
        var posted = TransferBuilder.PostedReceipt(transfer, "p", 900, (1, 3));
        var voided = TransferBuilder.BeginReceipt(transfer, "v", 901, (1, 2));
        transfer.VoidReceipt(voided.Id, "x", Now);

        Assert.Throws<ConflictException>(() => transfer.VoidReceipt(posted.Id, "x", Now));
        Assert.Throws<ConflictException>(() => transfer.VoidReceipt(voided.Id, "x", Now));
        Assert.Throws<ValidationException>(() => transfer.VoidReceipt(777, "x", Now));
    }

    [Fact]
    public void VoidedReceipts_ShouldNotBlockClose_AndCountNowhere()
    {
        var transfer = InState(TransferStatus.Dispatched);
        var receipt = TransferBuilder.BeginReceipt(transfer, "r", 900, (1, 3));
        transfer.VoidReceipt(receipt.Id, "x", Now);

        transfer.Close("lost", Now);

        Assert.Equal(TransferStatus.Closed, transfer.Status);
        Assert.Equal(10, transfer.Lines.Single(x => x.Id == 1).QtyClosedShort);
        Assert.Equal(0, transfer.Lines.Single(x => x.Id == 1).QtyReceived);
    }

    // Close / Cancel

    [Fact]
    public void Close_ShouldWriteOffTheInTransitRemainder_AtFrozenCost()
    {
        var transfer = InState(TransferStatus.PartiallyReceived);

        transfer.Close("damaged", Now.AddMinutes(3));

        var line1 = transfer.Lines.Single(x => x.Id == 1);
        var line2 = transfer.Lines.Single(x => x.Id == 2);

        Assert.Equal(TransferStatus.Closed, transfer.Status);
        Assert.Equal(7, line1.QtyClosedShort);
        Assert.Equal(4, line2.QtyClosedShort);
        Assert.Equal(0, transfer.TotalInTransitQuantity);
        Assert.Equal((7 + 4) * 5m, transfer.ClosedShortValueBase);
        Assert.Equal("damaged", transfer.ClosedReason);
        Assert.Equal(Now.AddMinutes(3), transfer.ClosedAt);
        Assert.Equal(11m * 5m, transfer.DomainEvents.OfType<TransferClosedEvent>().Single().ValueLostBase);
    }

    [Theory]
    [MemberData(nameof(StatusesThatCannotClose))]
    public void Close_ShouldConflict_FromAnyStateOtherThanDispatchedOrPartiallyReceived(TransferStatus status)
    {
        var transfer = InState(status);

        Assert.Throws<ConflictException>(() => transfer.Close("r", Now));
    }

    [Fact]
    public void Close_ShouldConflict_WhileAReceiptIsStillPosting()
    {
        var transfer = InState(TransferStatus.Dispatched);
        TransferBuilder.BeginReceipt(transfer, "r", 900, (1, 3));

        Assert.Throws<ConflictException>(() => transfer.Close("r", Now));
        Assert.Equal(TransferStatus.Dispatched, transfer.Status);
    }

    [Fact]
    public void Cancel_ShouldWork_OnlyFromDraft()
    {
        var transfer = InState(TransferStatus.Draft);

        transfer.Cancel("changed my mind", Now);

        Assert.Equal(TransferStatus.Cancelled, transfer.Status);
        Assert.Equal("changed my mind", transfer.CancelledReason);
        Assert.Equal(Now, transfer.CancelledAt);
        Assert.Single(transfer.DomainEvents.OfType<TransferCancelledEvent>());
    }

    [Theory]
    [MemberData(nameof(NonDraftStatuses))]
    public void Cancel_ShouldConflict_FromAnyNonDraftState_IncludingDispatched(TransferStatus status)
    {
        var transfer = InState(status);

        Assert.Throws<ConflictException>(() => transfer.Cancel("r", Now));
    }

    // TransferCode

    [Fact]
    public void TransferCode_ShouldRejectBlankAndOverlongValues()
    {
        Assert.Throws<ValidationException>(() => new TransferCode(" "));
        Assert.Throws<ValidationException>(() => new TransferCode(new string('X', TransferCode.MaxLength + 1)));
        Assert.Equal(new TransferCode("T1"), new TransferCode(" T1 "));
    }

    [Fact]
    public void TransferCode_Generate_ShouldFitTheColumn_AndAvoidAmbiguousLetters()
    {
        var code = TransferCode.Generate(Now).Value;

        Assert.True(code.Length <= TransferCode.MaxLength);
        Assert.DoesNotContain(code[9..], c => "ILOU".Contains(c));
    }
}
