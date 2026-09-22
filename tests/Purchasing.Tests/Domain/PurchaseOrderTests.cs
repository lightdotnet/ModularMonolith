using Light.Exceptions;
using Purchasing.Tests.TestSupport;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using StarterKit.Purchasing.Api.Domain.Suppliers;
using StarterKit.Purchasing.Contracts.Common;
using ValidationException = Light.Exceptions.ValidationException;

namespace Purchasing.Tests.Domain;

public class PurchaseOrderTests
{
    private static readonly DateTimeOffset Now = PurchasingBuilder.Now;

    private const string Requester = PurchasingBuilder.Requester;

    // Two lines: id 1 (10 units @ 5), id 2 (4 units @ 5).
    private static PurchaseOrder InState(PurchaseOrderStatus status)
    {
        switch (status)
        {
            case PurchaseOrderStatus.Draft:
                return PurchasingBuilder.DraftWithLines((1, 10), (2, 4));

            case PurchaseOrderStatus.PendingApproval:
                return PurchasingBuilder.Pending("wf-1", (1, 10), (2, 4));

            case PurchaseOrderStatus.Approved:
                return PurchasingBuilder.Approved((1, 10), (2, 4));

            case PurchaseOrderStatus.Rejected:
            {
                var order = PurchasingBuilder.Pending("wf-1", (1, 10), (2, 4));
                order.ApplyApprovalOutcome("wf-1", false, Now);
                return order;
            }

            case PurchaseOrderStatus.PartiallyReceived:
            {
                var order = PurchasingBuilder.Approved((1, 10), (2, 4));
                order.ApplyReceipt([(1, 3)], Now.AddMinutes(2));
                return order;
            }

            case PurchaseOrderStatus.Received:
            {
                var order = PurchasingBuilder.Approved((1, 10), (2, 4));
                order.ApplyReceipt([(1, 10), (2, 4)], Now.AddMinutes(2));
                return order;
            }

            case PurchaseOrderStatus.Closed:
            {
                var order = InState(PurchaseOrderStatus.PartiallyReceived);
                order.Close("gave up", "mgr", Now, false);
                return order;
            }

            case PurchaseOrderStatus.Cancelled:
            {
                var order = PurchasingBuilder.DraftWithLines((1, 10));
                order.Cancel("no", Requester, false, Now, false);
                return order;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
    }

    public static TheoryData<PurchaseOrderStatus> NotEditable() =>
        new(Enum.GetValues<PurchaseOrderStatus>().Except([PurchaseOrderStatus.Draft, PurchaseOrderStatus.Rejected]));

    public static TheoryData<PurchaseOrderStatus> NotSubmittable() =>
        new(Enum.GetValues<PurchaseOrderStatus>().Except([PurchaseOrderStatus.Draft, PurchaseOrderStatus.Rejected]));

    public static TheoryData<PurchaseOrderStatus> NotReceivable() =>
        new(Enum.GetValues<PurchaseOrderStatus>().Except([PurchaseOrderStatus.Approved, PurchaseOrderStatus.PartiallyReceived]));

    // Supplier

    [Fact]
    public void Supplier_ShouldNormalizeTheCode_AndToggleStatus()
    {
        var supplier = Supplier.Create("  ab-1 ", "  Acme ", null, null, null, null, "Net 30");

        Assert.Equal("AB-1", supplier.Code);
        Assert.Equal("Acme", supplier.Name);
        Assert.True(supplier.IsActive);

        supplier.Deactivate();
        Assert.Equal(SupplierStatus.Inactive, supplier.Status);
        Assert.False(supplier.IsActive);

        supplier.Activate();
        Assert.True(supplier.IsActive);

        supplier.Update(" x9 ", "New", "c", "p", "e", "a", "t");
        Assert.Equal("X9", supplier.Code);
        Assert.Equal("c", supplier.ContactName);
        Assert.Equal("AB-1", Supplier.NormalizeCode(" ab-1"));
    }

    // Create / edit

    [Theory]
    [InlineData("", "e")]
    [InlineData(" ", "e")]
    [InlineData("u", "")]
    [InlineData("u", " ")]
    public void Create_ShouldRequireRequesterUserAndEmployee(
        string user,
        string employee)
    {
        Assert.Throws<ValidationException>(() =>
            PurchaseOrder.Create(1, "S", "l", "L", null, user, employee, null, Now));
    }

    [Fact]
    public void Create_ShouldStartAsDraft_WithGeneratedNumber_AndTrimmedLocation()
    {
        var order = PurchaseOrder.Create(1, "S", " loc ", "L", null, "u", "e", "n", Now);

        Assert.Equal(PurchaseOrderStatus.Draft, order.Status);
        Assert.Equal("loc", order.LocationId);
        Assert.False(string.IsNullOrWhiteSpace(order.PONumber.Value));
    }

    [Theory]
    [InlineData(PurchaseOrderStatus.Draft)]
    [InlineData(PurchaseOrderStatus.Rejected)]
    public void Editing_ShouldBeAllowed_InDraftAndRejected(PurchaseOrderStatus status)
    {
        var order = InState(status);

        order.UpdateHeader(2, "S2", " l2 ", "L2", Now, "note");
        order.AddLine(9, "P9", "S9", 2, PurchasingBuilder.Cost(1m));
        order.UpdateLine(1, 20, PurchasingBuilder.Cost(6m));
        order.RemoveLine(2);

        Assert.Equal("l2", order.LocationId);
        Assert.Equal(20, order.Lines.Single(x => x.Id == 1).OrderedQuantity);
        Assert.Equal(6m, order.Lines.Single(x => x.Id == 1).UnitCostAmount);
        Assert.Equal(2, order.Lines.Count);
    }

    [Theory]
    [MemberData(nameof(NotEditable))]
    public void Editing_ShouldConflict_FromAnyOtherState(PurchaseOrderStatus status)
    {
        var order = InState(status);

        Assert.Throws<ConflictException>(() => order.UpdateHeader(2, "S", "l", "L", null, null));
        Assert.Throws<ConflictException>(() => order.AddLine(99, "P", "S", 1, PurchasingBuilder.Cost(1m)));
        Assert.Throws<ConflictException>(() => order.UpdateLine(1, 1, PurchasingBuilder.Cost(1m)));
        Assert.Throws<ConflictException>(() => order.RemoveLine(1));
    }

    [Fact]
    public void AddLine_ShouldRejectDuplicateProducts_NonPositiveQuantities_AndUnknownLines()
    {
        var order = InState(PurchaseOrderStatus.Draft);

        Assert.Throws<ValidationException>(() => order.AddLine(1, "P", "S", 1, PurchasingBuilder.Cost(1m)));
        Assert.Throws<ValidationException>(() => order.AddLine(50, "P", "S", 0, PurchasingBuilder.Cost(1m)));
        Assert.Throws<ValidationException>(() => order.UpdateLine(1, 0, PurchasingBuilder.Cost(1m)));
        Assert.Throws<ValidationException>(() => order.UpdateLine(999, 1, PurchasingBuilder.Cost(1m)));
        Assert.Throws<ValidationException>(() => order.RemoveLine(999));
    }

    [Fact]
    public void Totals_ShouldSumLines_AndSaturateInsteadOfOverflowing()
    {
        var order = InState(PurchaseOrderStatus.Draft);

        Assert.Equal(70m, order.TotalAmount.Amount);
        Assert.Equal(14, order.TotalOrderedQuantity);

        var huge = PurchasingBuilder.Draft();
        huge.AddLine(1, "P", "S", int.MaxValue, PurchasingBuilder.Cost(decimal.MaxValue / 2));
        huge.AddLine(2, "P", "S", 1, PurchasingBuilder.Cost(decimal.MaxValue / 2));
        huge.AddLine(3, "P", "S", 1, PurchasingBuilder.Cost(decimal.MaxValue / 2));

        Assert.Equal(decimal.MaxValue, huge.Lines[0].LineTotal.Amount);
        Assert.Equal(decimal.MaxValue, huge.TotalAmount.Amount);
    }

    // Submit / withdraw

    [Fact]
    public void Submit_ShouldMoveToPendingApproval_AndRaiseAnEvent()
    {
        var order = InState(PurchaseOrderStatus.Draft);

        order.Submit(Requester, "wf-1", "emp-a", "Approver", Now);

        Assert.Equal(PurchaseOrderStatus.PendingApproval, order.Status);
        Assert.Equal("wf-1", order.ApprovalRequestId);
        Assert.Equal("emp-a", order.ApproverEmployeeId);
        Assert.Equal(Now, order.SubmittedAt);
        Assert.Single(order.DomainEvents.OfType<PurchaseOrderSubmittedEvent>());
    }

    [Fact]
    public void Submit_ShouldBeRequesterOnly_EvenForAManager()
    {
        var order = InState(PurchaseOrderStatus.Draft);

        Assert.Throws<ForbiddenException>(() => order.Submit("someone-else", "wf-1", "e", null, Now));
        Assert.Throws<ForbiddenException>(() => order.EnsureCanSubmit("someone-else"));
    }

    [Fact]
    public void Submit_ShouldRequireAtLeastOneLine_AndAnApprovalRequestId()
    {
        var empty = PurchasingBuilder.Draft();

        Assert.Throws<ValidationException>(() => empty.Submit(Requester, "wf", "e", null, Now));
        Assert.Throws<ValidationException>(() => InState(PurchaseOrderStatus.Draft).Submit(Requester, " ", "e", null, Now));
    }

    [Theory]
    [MemberData(nameof(NotSubmittable))]
    public void Submit_ShouldConflict_FromAnyStateOtherThanDraftOrRejected(PurchaseOrderStatus status)
    {
        var order = InState(status);

        Assert.Throws<ConflictException>(() => order.Submit(Requester, "wf-x", "e", null, Now));
    }

    [Fact]
    public void Resubmit_AfterRejection_ShouldCreateANewWorkflow_AndClearRejectedAt()
    {
        var order = InState(PurchaseOrderStatus.Rejected);
        Assert.NotNull(order.RejectedAt);

        order.Submit(Requester, "wf-2", "emp-b", "B", Now.AddMinutes(10));

        Assert.Equal(PurchaseOrderStatus.PendingApproval, order.Status);
        Assert.Equal("wf-2", order.ApprovalRequestId);
        Assert.Null(order.RejectedAt);
    }

    [Fact]
    public void Withdraw_ShouldReturnToDraft_AndClearTheWorkflow_SoLateEventsAreDropped()
    {
        var order = InState(PurchaseOrderStatus.PendingApproval);

        order.Withdraw(Requester);

        Assert.Equal(PurchaseOrderStatus.Draft, order.Status);
        Assert.Null(order.ApprovalRequestId);
        Assert.Null(order.SubmittedAt);
        Assert.False(order.ApplyApprovalOutcome("wf-1", true, Now));
        Assert.Equal(PurchaseOrderStatus.Draft, order.Status);
    }

    [Fact]
    public void Withdraw_ShouldBeRequesterOnly_AndOnlyWhenPending()
    {
        Assert.Throws<ForbiddenException>(() => InState(PurchaseOrderStatus.PendingApproval).Withdraw("other"));
        Assert.Throws<ConflictException>(() => InState(PurchaseOrderStatus.Draft).Withdraw(Requester));
        Assert.Throws<ConflictException>(() => InState(PurchaseOrderStatus.Approved).EnsureCanWithdraw(Requester));
    }

    // Approval outcome

    [Fact]
    public void ApplyApprovalOutcome_ShouldApprove_AndBeIdempotent()
    {
        var order = InState(PurchaseOrderStatus.PendingApproval);

        Assert.True(order.ApplyApprovalOutcome("wf-1", true, Now.AddMinutes(1)));
        Assert.False(order.ApplyApprovalOutcome("wf-1", true, Now.AddMinutes(2)));

        Assert.Equal(PurchaseOrderStatus.Approved, order.Status);
        Assert.Equal(Now.AddMinutes(1), order.ApprovedAt);
        Assert.Single(order.DomainEvents.OfType<PurchaseOrderApprovedEvent>());
    }

    [Fact]
    public void ApplyApprovalOutcome_ShouldReject_AndRecordRejectedAt()
    {
        var order = InState(PurchaseOrderStatus.PendingApproval);

        Assert.True(order.ApplyApprovalOutcome("wf-1", false, Now.AddMinutes(1)));

        Assert.Equal(PurchaseOrderStatus.Rejected, order.Status);
        Assert.Equal(Now.AddMinutes(1), order.RejectedAt);
        Assert.Single(order.DomainEvents.OfType<PurchaseOrderRejectedEvent>());
    }

    [Fact]
    public void ApplyApprovalOutcome_ShouldIgnoreAMismatchedOrSupersededWorkflow()
    {
        var order = InState(PurchaseOrderStatus.Rejected);
        order.Submit(Requester, "wf-2", "e", null, Now.AddMinutes(5));

        // Late decision from the first workflow.
        Assert.False(order.ApplyApprovalOutcome("wf-1", true, Now.AddMinutes(6)));
        Assert.Equal(PurchaseOrderStatus.PendingApproval, order.Status);

        Assert.True(order.ApplyApprovalOutcome("wf-2", true, Now.AddMinutes(7)));
        Assert.Equal(PurchaseOrderStatus.Approved, order.Status);
    }

    [Theory]
    [InlineData(PurchaseOrderStatus.Draft)]
    [InlineData(PurchaseOrderStatus.Approved)]
    [InlineData(PurchaseOrderStatus.Rejected)]
    [InlineData(PurchaseOrderStatus.PartiallyReceived)]
    public void ApplyApprovalOutcome_ShouldIgnoreAnOrderThatIsNotPendingApproval(PurchaseOrderStatus status)
    {
        var order = InState(status);

        Assert.False(order.ApplyApprovalOutcome("wf-1", true, Now.AddHours(1)));
        Assert.False(order.ApplyApprovalOutcome("wf-1", false, Now.AddHours(1)));
        Assert.Equal(status, order.Status);
    }

    // Receive

    [Fact]
    public void ApplyReceipt_ShouldBecomePartiallyReceived_ThenReceived()
    {
        var order = InState(PurchaseOrderStatus.Approved);

        order.ApplyReceipt([(1, 4)], Now.AddMinutes(2));

        Assert.Equal(PurchaseOrderStatus.PartiallyReceived, order.Status);
        Assert.Equal(4, order.TotalReceivedQuantity);
        Assert.Equal(10, order.TotalOutstandingQuantity);
        Assert.Null(order.ReceivedAt);

        order.ApplyReceipt([(1, 6), (2, 4)], Now.AddMinutes(3));

        Assert.Equal(PurchaseOrderStatus.Received, order.Status);
        Assert.Equal(Now.AddMinutes(3), order.ReceivedAt);
        Assert.Equal(2, order.DomainEvents.OfType<PurchaseOrderReceivedEvent>().Count());
    }

    [Fact]
    public void ApplyReceipt_ShouldBeAtomic_WhenALaterLineOverReceives()
    {
        var order = InState(PurchaseOrderStatus.Approved);

        Assert.Throws<ValidationException>(() => order.ApplyReceipt([(1, 5), (2, 5)], Now));

        Assert.Equal(0, order.TotalReceivedQuantity);
        Assert.Equal(PurchaseOrderStatus.Approved, order.Status);
    }

    [Fact]
    public void EnsureCanReceive_ShouldRejectBadLines_AndCountInFlightQuantity()
    {
        var order = InState(PurchaseOrderStatus.Approved);
        var none = new Dictionary<long, int>();

        Assert.Throws<ValidationException>(() => order.EnsureCanReceive([], none));
        Assert.Throws<ValidationException>(() => order.EnsureCanReceive([(1, 1), (1, 2)], none));
        Assert.Throws<ValidationException>(() => order.EnsureCanReceive([(1, 0)], none));
        Assert.Throws<ValidationException>(() => order.EnsureCanReceive([(999, 1)], none));
        Assert.Throws<ValidationException>(() => order.EnsureCanReceive([(1, 11)], none));

        var inFlight = new Dictionary<long, int> { [1] = 8 };
        Assert.Throws<ValidationException>(() => order.EnsureCanReceive([(1, 3)], inFlight));
        order.EnsureCanReceive([(1, 2)], inFlight);
    }

    [Theory]
    [MemberData(nameof(NotReceivable))]
    public void Receive_ShouldConflict_FromAnyStateOtherThanApprovedOrPartiallyReceived(PurchaseOrderStatus status)
    {
        var order = InState(status);

        Assert.Throws<ConflictException>(() => order.EnsureCanReceive([(1, 1)], new Dictionary<long, int>()));
        Assert.Throws<ConflictException>(() => order.ApplyReceipt([(1, 1)], Now));
    }

    [Fact]
    public void EnsureReceivedNotBeforeApproval_ShouldRejectAnEarlierDate_ButNotWhenNeverApproved()
    {
        var approved = InState(PurchaseOrderStatus.Approved);

        Assert.Throws<ValidationException>(() => approved.EnsureReceivedNotBeforeApproval(approved.ApprovedAt!.Value.AddMinutes(-1)));
        approved.EnsureReceivedNotBeforeApproval(approved.ApprovedAt!.Value);
        InState(PurchaseOrderStatus.Draft).EnsureReceivedNotBeforeApproval(Now.AddYears(-5));
    }

    // Returns (informational)

    [Fact]
    public void RegisterReturn_ShouldBeInformational_NeverReopenTheOrder_AndNotExceedReceived()
    {
        var order = InState(PurchaseOrderStatus.Received);

        order.RegisterReturn(1, 4);

        var line = order.Lines.Single(x => x.Id == 1);
        Assert.Equal(4, line.ReturnedQuantity);
        Assert.Equal(0, line.OutstandingQuantity);
        Assert.Equal(PurchaseOrderStatus.Received, order.Status);
        Assert.Throws<ValidationException>(() => order.RegisterReturn(1, 7));
        Assert.Throws<ValidationException>(() => order.RegisterReturn(1, 0));
        Assert.Throws<ValidationException>(() => order.RegisterReturn(999, 1));
        order.RegisterReturn(1, 6);
        Assert.Equal(10, line.ReturnedQuantity);
    }

    // Close

    [Fact]
    public void Close_ShouldGiveUpTheRemainder_AndRecordTheActor()
    {
        var order = InState(PurchaseOrderStatus.PartiallyReceived);

        order.Close("supplier stopped", "mgr-1", Now.AddDays(1), hasReceiptInFlight: false);

        Assert.Equal(PurchaseOrderStatus.Closed, order.Status);
        Assert.Equal("supplier stopped", order.ClosedReason);
        Assert.Equal("mgr-1", order.ClosedBy);
        Assert.Equal(Now.AddDays(1), order.ClosedAt);
    }

    [Fact]
    public void Close_ShouldConflict_WhileAReceiptIsInFlight_AndFromOtherStates()
    {
        Assert.Throws<ConflictException>(() =>
            InState(PurchaseOrderStatus.PartiallyReceived).Close("r", "u", Now, hasReceiptInFlight: true));

        foreach (var status in Enum.GetValues<PurchaseOrderStatus>().Where(x => x != PurchaseOrderStatus.PartiallyReceived))
            Assert.Throws<ConflictException>(() => InState(status).Close("r", "u", Now, false));
    }

    // Cancel

    [Theory]
    [InlineData(PurchaseOrderStatus.Draft)]
    [InlineData(PurchaseOrderStatus.Rejected)]
    public void Cancel_ShouldAllowTheRequester_ForDraftAndRejected(PurchaseOrderStatus status)
    {
        var order = InState(status);

        order.Cancel("no longer", Requester, canManage: false, Now, false);

        Assert.Equal(PurchaseOrderStatus.Cancelled, order.Status);
        Assert.Equal(Requester, order.CancelledBy);
        Assert.Equal("no longer", order.CancelledReason);
        Assert.Single(order.DomainEvents.OfType<PurchaseOrderCancelledEvent>());
    }

    [Fact]
    public void Cancel_ShouldAllowAManager_ForSomeoneElsesDraft_ButRefuseAnUnrelatedUser()
    {
        Assert.Throws<ForbiddenException>(() =>
            InState(PurchaseOrderStatus.Draft).Cancel("r", "stranger", canManage: false, Now, false));

        var order = InState(PurchaseOrderStatus.Draft);
        order.Cancel("r", "manager", canManage: true, Now, false);

        Assert.Equal(PurchaseOrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_OfAnApprovedOrder_ShouldNeedAManager_EvenForTheRequester()
    {
        Assert.Throws<ForbiddenException>(() =>
            InState(PurchaseOrderStatus.Approved).Cancel("r", Requester, canManage: false, Now, false));

        var order = InState(PurchaseOrderStatus.Approved);
        order.Cancel("r", "mgr", canManage: true, Now, false);

        Assert.Equal(PurchaseOrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_OfAnApprovedOrder_ShouldConflict_WhenSomethingWasReceivedOrIsInFlight()
    {
        Assert.Throws<ConflictException>(() =>
            InState(PurchaseOrderStatus.Approved).Cancel("r", "mgr", true, Now, hasReceiptInFlight: true));
        Assert.Throws<ConflictException>(() =>
            InState(PurchaseOrderStatus.PartiallyReceived).Cancel("r", "mgr", true, Now, false));
    }

    [Fact]
    public void Cancel_ShouldConflict_ForPendingAndTerminalStates()
    {
        Assert.Throws<ConflictException>(() =>
            InState(PurchaseOrderStatus.PendingApproval).Cancel("r", Requester, true, Now, false));

        foreach (var status in new[]
        {
            PurchaseOrderStatus.PartiallyReceived,
            PurchaseOrderStatus.Received,
            PurchaseOrderStatus.Closed,
            PurchaseOrderStatus.Cancelled,
        })
        {
            Assert.Throws<ConflictException>(() => InState(status).Cancel("r", Requester, true, Now, false));
        }
    }

    [Fact]
    public void EnsureOwnerOrManager_ShouldAcceptTheRequesterOrAManagerOnly()
    {
        var order = InState(PurchaseOrderStatus.Draft);

        order.EnsureOwnerOrManager(Requester, false);
        order.EnsureOwnerOrManager("other", true);
        Assert.Throws<ForbiddenException>(() => order.EnsureOwnerOrManager("other", false));
    }
}
