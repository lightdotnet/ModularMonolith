using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Purchasing.Tests.TestSupport;
using StarterKit.Approval.Contracts.Approvals;
using StarterKit.Approval.Contracts.Services;
using StarterKit.Organization.Contracts.Services;
using StarterKit.Purchasing.Api.Application.PurchaseOrders;
using StarterKit.Purchasing.Api.Application.PurchaseOrders.Commands;
using StarterKit.Purchasing.Api.Application.PurchaseOrders.EventHandlers;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using ValidationException = Light.Exceptions.ValidationException;

namespace Purchasing.Tests.Application.PurchaseOrders;

/// <summary>Submit / withdraw handlers, the approval-finalized subscriber and the approval sweep, with Approval and Organization mocked.</summary>
public class PurchaseOrderApprovalFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const string Requester = PurchasingBuilder.Requester;

    private static Mock<IApprovalService> MakeApproval(
        string? createdId = "wf-new",
        string? createError = null)
    {
        var mock = new Mock<IApprovalService>();

        mock.Setup(x => x.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createError is null
                ? Result<string>.Success(createdId!)
                : Result<string>.Error(createError));

        mock.Setup(x => x.CancelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        return mock;
    }

    private static Mock<IOrgDirectoryService> MakeOrg(params string[] candidateEmployeeIds)
    {
        var mock = new Mock<IOrgDirectoryService>();

        mock.Setup(x => x.GetApproverCandidatesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidateEmployeeIds
                .Select(x => new ResolvedApproverDto { EmployeeId = x, UserId = $"user-{x}", Name = $"Name {x}" })
                .ToList());

        mock.Setup(x => x.GetEmployeeNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Requester Name");

        return mock;
    }

    private static SubmitPurchaseOrderCommandHandler MakeSubmit(
        PurchasingTestHost host,
        Mock<IApprovalService> approval,
        Mock<IOrgDirectoryService> org) =>
        new(
            host.Context,
            new PurchaseOrderApprovalCoordinator(approval.Object, org.Object),
            approval.Object,
            host.DateTime,
            NullLogger<SubmitPurchaseOrderCommandHandler>.Instance);

    private static SubmitPurchaseOrderCommand SubmitCommand(
        long id,
        string approver = "emp-a",
        string user = Requester) =>
        new(id, new SubmitPurchaseOrderRequest { ApproverEmployeeId = approver }, user);

    private static async Task<PurchaseOrder> ReloadAsync(
        PurchasingTestHost host,
        long id)
    {
        PurchasingSeed.Detach(host);

        return await host.Context.PurchaseOrders.Include(x => x.Lines).SingleAsync(x => x.Id == id, Ct);
    }

    // Submit

    [Fact]
    public async Task Submit_ShouldReturnNotFound_ForAnUnknownOrder()
    {
        using var host = new PurchasingTestHost();

        var result = await MakeSubmit(host, MakeApproval(), MakeOrg("emp-a")).Handle(SubmitCommand(999), Ct);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Submit_ShouldMoveToPendingApproval_RecordingTheWorkflowAndTheChosenApprover()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.DraftAsync(host);
        var approval = MakeApproval("wf-42");

        var result = await MakeSubmit(host, approval, MakeOrg("emp-a", "emp-b")).Handle(SubmitCommand(order.Id, "emp-b"), Ct);

        Assert.True(result.IsSuccess);
        var stored = await ReloadAsync(host, order.Id);
        Assert.Equal(PurchaseOrderStatus.PendingApproval, stored.Status);
        Assert.Equal("wf-42", stored.ApprovalRequestId);
        Assert.Equal("emp-b", stored.ApproverEmployeeId);
        Assert.Equal("Name emp-b", stored.ApproverName);
        approval.Verify(
            x => x.CreateAsync(
                It.Is<CreateApprovalRequest>(r =>
                    r.RequestType == "PurchaseOrder"
                    && r.RequestId == order.Id.ToString()
                    && r.RequesterUserId == Requester
                    && r.ApproverChain.Count == 1
                    && r.ApproverChain[0].ApproverEmployeeId == "emp-b"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Submit_ShouldThrowForbidden_ForANonRequester_WithoutCreatingAWorkflow()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.DraftAsync(host);
        var approval = MakeApproval();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            MakeSubmit(host, approval, MakeOrg("emp-a")).Handle(SubmitCommand(order.Id, user: "someone-else"), Ct));

        approval.Verify(x => x.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Submit_ShouldRejectAnOrderWithoutLines_WithoutCreatingAWorkflow()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.EmptyDraftAsync(host);
        var approval = MakeApproval();

        await Assert.ThrowsAsync<ValidationException>(() =>
            MakeSubmit(host, approval, MakeOrg("emp-a")).Handle(SubmitCommand(order.Id), Ct));

        approval.Verify(x => x.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Submit_ShouldRejectANonSubmittableState_WithoutCreatingAWorkflow()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.PendingAsync(host);
        var approval = MakeApproval();

        await Assert.ThrowsAsync<ConflictException>(() =>
            MakeSubmit(host, approval, MakeOrg("emp-a")).Handle(SubmitCommand(order.Id), Ct));

        approval.Verify(x => x.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Submit_ShouldRequireTheApproverToBeACandidate_AndAnyCandidateToExist()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.DraftAsync(host);
        var approval = MakeApproval();

        var notCandidate = await MakeSubmit(host, approval, MakeOrg("emp-a")).Handle(SubmitCommand(order.Id, "emp-x"), Ct);
        var noCandidates = await MakeSubmit(host, approval, MakeOrg()).Handle(SubmitCommand(order.Id, "emp-a"), Ct);

        Assert.False(notCandidate.IsSuccess);
        Assert.Contains("Invalid approver selection", notCandidate.Message);
        Assert.False(noCandidates.IsSuccess);
        approval.Verify(x => x.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(PurchaseOrderStatus.Draft, (await ReloadAsync(host, order.Id)).Status);
    }

    [Fact]
    public async Task Submit_ShouldReturnAGenericError_WhenApprovalRefusesToCreateTheWorkflow()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.DraftAsync(host);
        var approval = MakeApproval(createError: "internal approval detail");

        var result = await MakeSubmit(host, approval, MakeOrg("emp-a")).Handle(SubmitCommand(order.Id), Ct);

        Assert.False(result.IsSuccess);
        Assert.Equal("Failed to submit the purchase order for approval.", result.Message);
        Assert.DoesNotContain("internal approval detail", result.Message);
        Assert.Equal(PurchaseOrderStatus.Draft, (await ReloadAsync(host, order.Id)).Status);
    }

    [Fact]
    public async Task Submit_ShouldCancelTheJustCreatedWorkflow_WhenTheLocalSaveFails()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.DraftAsync(host);
        var approval = MakeApproval("wf-orphan");
        host.SaveFaults.Arm(failTimes: 1);

        await Assert.ThrowsAsync<ConflictException>(() =>
            MakeSubmit(host, approval, MakeOrg("emp-a")).Handle(SubmitCommand(order.Id), Ct));

        approval.Verify(x => x.CancelAsync("wf-orphan", Requester, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(PurchaseOrderStatus.Draft, (await ReloadAsync(host, order.Id)).Status);
    }

    [Fact]
    public async Task Submit_ShouldStillRethrowTheOriginalFailure_WhenTheCompensationItselfFails()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.DraftAsync(host);
        var approval = MakeApproval("wf-orphan");
        approval
            .Setup(x => x.CancelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("approval down"));
        host.SaveFaults.Arm(failTimes: 1);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            MakeSubmit(host, approval, MakeOrg("emp-a")).Handle(SubmitCommand(order.Id), Ct));

        Assert.Contains("modified by another user", ex.Message);
    }

    [Fact]
    public async Task Submit_AfterARejection_ShouldCreateANewWorkflow()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.PendingAsync(host, "wf-old");
        order.ApplyApprovalOutcome("wf-old", false, host.DateTime.UtcNow);
        await host.Context.SaveChangesAsync(Ct);
        var approval = MakeApproval("wf-new");

        var result = await MakeSubmit(host, approval, MakeOrg("emp-a")).Handle(SubmitCommand(order.Id), Ct);

        Assert.True(result.IsSuccess);
        var stored = await ReloadAsync(host, order.Id);
        Assert.Equal(PurchaseOrderStatus.PendingApproval, stored.Status);
        Assert.Equal("wf-new", stored.ApprovalRequestId);
        // A late event for the first workflow is now harmless.
        Assert.False(stored.ApplyApprovalOutcome("wf-old", true, host.DateTime.UtcNow));
    }

    // Withdraw

    private static WithdrawPurchaseOrderCommandHandler MakeWithdraw(
        PurchasingTestHost host,
        Mock<IApprovalService> approval) =>
        new(host.Context, approval.Object, host.DateTime);

    [Fact]
    public async Task Withdraw_ShouldCancelTheWorkflow_AndReturnToDraft()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.PendingAsync(host, "wf-1");
        var approval = MakeApproval();

        var result = await MakeWithdraw(host, approval).Handle(new WithdrawPurchaseOrderCommand(order.Id, Requester), Ct);

        Assert.True(result.IsSuccess);
        approval.Verify(x => x.CancelAsync("wf-1", Requester, It.IsAny<CancellationToken>()), Times.Once);
        var stored = await ReloadAsync(host, order.Id);
        Assert.Equal(PurchaseOrderStatus.Draft, stored.Status);
        Assert.Null(stored.ApprovalRequestId);
    }

    [Fact]
    public async Task Withdraw_ShouldGuardTheCaller_TheStateAndUnknownOrders()
    {
        using var host = new PurchasingTestHost();
        var pending = await PurchasingSeed.PendingAsync(host, "wf-1");
        var draft = await PurchasingSeed.DraftAsync(host);
        var approval = MakeApproval();
        var handler = MakeWithdraw(host, approval);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new WithdrawPurchaseOrderCommand(pending.Id, "other"), Ct));
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new WithdrawPurchaseOrderCommand(draft.Id, Requester), Ct));
        Assert.False((await handler.Handle(new WithdrawPurchaseOrderCommand(999, Requester), Ct)).IsSuccess);
        approval.Verify(x => x.CancelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Withdraw_ShouldRecover_WhenApprovalWasAlreadyCancelledByAnEarlierAttempt()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.PendingAsync(host, "wf-1");
        var approval = MakeApproval();
        approval
            .Setup(x => x.CancelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Error("already cancelled"));
        approval
            .Setup(x => x.GetStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalStatusView("wf-1", "PurchaseOrder", order.Id.ToString(), ApprovalStatus.Cancelled, 0));

        var result = await MakeWithdraw(host, approval).Handle(new WithdrawPurchaseOrderCommand(order.Id, Requester), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(PurchaseOrderStatus.Draft, (await ReloadAsync(host, order.Id)).Status);
    }

    [Fact]
    public async Task Withdraw_ShouldApplyTheDecision_AndReportAConflict_WhenApprovalAlreadyDecided()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.PendingAsync(host, "wf-1");
        var approval = MakeApproval();
        approval
            .Setup(x => x.CancelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Error("already decided"));
        approval
            .Setup(x => x.GetStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalStatusView("wf-1", "PurchaseOrder", order.Id.ToString(), ApprovalStatus.Approved, 1));

        var result = await MakeWithdraw(host, approval).Handle(new WithdrawPurchaseOrderCommand(order.Id, Requester), Ct);

        Assert.False(result.IsSuccess);
        Assert.Contains("can no longer be withdrawn", result.Message);
        Assert.Equal(PurchaseOrderStatus.Approved, (await ReloadAsync(host, order.Id)).Status);
    }

    [Fact]
    public async Task Withdraw_ShouldIgnoreADecisionOfASupersededWorkflow_AndStayPending()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.PendingAsync(host, "wf-2");
        var approval = MakeApproval();
        approval
            .Setup(x => x.CancelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Error("nope"));
        approval
            .Setup(x => x.GetStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApprovalStatusView("wf-1", "PurchaseOrder", order.Id.ToString(), ApprovalStatus.Approved, 1));

        var result = await MakeWithdraw(host, approval).Handle(new WithdrawPurchaseOrderCommand(order.Id, Requester), Ct);

        Assert.False(result.IsSuccess);
        Assert.Equal(PurchaseOrderStatus.PendingApproval, (await ReloadAsync(host, order.Id)).Status);
    }

    [Fact]
    public async Task Withdraw_ShouldReportAConflict_WhenApprovalHasNoStatusAtAll()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.PendingAsync(host, "wf-1");
        var approval = MakeApproval();
        approval
            .Setup(x => x.CancelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Error("nope"));

        var result = await MakeWithdraw(host, approval).Handle(new WithdrawPurchaseOrderCommand(order.Id, Requester), Ct);

        Assert.False(result.IsSuccess);
        Assert.Equal(PurchaseOrderStatus.PendingApproval, (await ReloadAsync(host, order.Id)).Status);
    }

    // Integration event subscriber

    private static ApprovalFinalizedIntegrationEventHandler MakeEventHandler(PurchasingTestHost host) =>
        new(host.Context, host.DateTime, NullLogger<ApprovalFinalizedIntegrationEventHandler>.Instance);

    [Theory]
    [InlineData(ApprovalStatus.Approved, PurchaseOrderStatus.Approved)]
    [InlineData(ApprovalStatus.Rejected, PurchaseOrderStatus.Rejected)]
    public async Task EventHandler_ShouldApplyATerminalDecision(
        ApprovalStatus status,
        PurchaseOrderStatus expected)
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.PendingAsync(host, "wf-1");

        await MakeEventHandler(host).Handle(
            new ApprovalFinalizedIntegrationEvent("PurchaseOrder", order.Id.ToString(), "wf-1", status),
            Ct);

        Assert.Equal(expected, (await ReloadAsync(host, order.Id)).Status);
    }

    [Fact]
    public async Task EventHandler_ShouldIgnoreOtherRequestTypes_BadIds_UnknownOrders_AndSupersededWorkflows()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.PendingAsync(host, "wf-2");
        var handler = MakeEventHandler(host);

        await handler.Handle(new ApprovalFinalizedIntegrationEvent("LeaveRequest", order.Id.ToString(), "wf-2", ApprovalStatus.Approved), Ct);
        await handler.Handle(new ApprovalFinalizedIntegrationEvent("PurchaseOrder", "not-a-number", "wf-2", ApprovalStatus.Approved), Ct);
        await handler.Handle(new ApprovalFinalizedIntegrationEvent("PurchaseOrder", "9999", "wf-2", ApprovalStatus.Approved), Ct);
        await handler.Handle(new ApprovalFinalizedIntegrationEvent("PurchaseOrder", order.Id.ToString(), "wf-1", ApprovalStatus.Approved), Ct);
        await handler.Handle(new ApprovalFinalizedIntegrationEvent("PurchaseOrder", order.Id.ToString(), "wf-2", ApprovalStatus.Cancelled), Ct);

        Assert.Equal(PurchaseOrderStatus.PendingApproval, (await ReloadAsync(host, order.Id)).Status);
    }

    [Fact]
    public async Task EventHandler_ShouldBeIdempotent_AndNeverUnFinalizeAnOrder()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.PendingAsync(host, "wf-1");
        var handler = MakeEventHandler(host);

        await handler.Handle(new ApprovalFinalizedIntegrationEvent("PurchaseOrder", order.Id.ToString(), "wf-1", ApprovalStatus.Approved), Ct);
        await handler.Handle(new ApprovalFinalizedIntegrationEvent("PurchaseOrder", order.Id.ToString(), "wf-1", ApprovalStatus.Rejected), Ct);

        Assert.Equal(PurchaseOrderStatus.Approved, (await ReloadAsync(host, order.Id)).Status);
    }

    [Fact]
    public async Task EventHandler_ShouldSwallowFailures_SoTheOriginatingDecideNeverFails()
    {
        using var host = new PurchasingTestHost();
        var order = await PurchasingSeed.PendingAsync(host, "wf-1");
        host.SaveFaults.Arm(failTimes: 1);

        await MakeEventHandler(host).Handle(
            new ApprovalFinalizedIntegrationEvent("PurchaseOrder", order.Id.ToString(), "wf-1", ApprovalStatus.Approved),
            Ct);

        Assert.Equal(PurchaseOrderStatus.PendingApproval, (await ReloadAsync(host, order.Id)).Status);
    }

    // Sweep

    private static Mock<IApprovalService> MakeSweepApproval(params (long OrderId, string WorkflowId, ApprovalStatus Status)[] views)
    {
        var mock = new Mock<IApprovalService>();

        mock.Setup(x => x.GetStatusesAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(views.ToDictionary(
                x => x.WorkflowId,
                x => new ApprovalStatusView(x.WorkflowId, "PurchaseOrder", x.OrderId.ToString(), x.Status, 0)));

        return mock;
    }

    private static Task<int> SweepAsync(
        PurchasingTestHost host,
        Mock<IApprovalService> approval,
        int batchSize = 200) =>
        PurchaseOrderApprovalReconciliationService.ReconcileOnceAsync(host.Context, approval.Object, host.DateTime, batchSize, Ct);

    [Fact]
    public async Task Sweep_ShouldApplyApprovedAndRejectedDecisions_InOneBatch()
    {
        using var host = new PurchasingTestHost();
        var a = await PurchasingSeed.PendingAsync(host, "wf-a");
        var b = await PurchasingSeed.PendingAsync(host, "wf-b");
        var c = await PurchasingSeed.PendingAsync(host, "wf-c");
        var approval = MakeSweepApproval(
            (a.Id, "wf-a", ApprovalStatus.Approved),
            (b.Id, "wf-b", ApprovalStatus.Rejected),
            (c.Id, "wf-c", ApprovalStatus.Pending));

        var changed = await SweepAsync(host, approval);

        Assert.Equal(2, changed);
        Assert.Equal(PurchaseOrderStatus.Approved, (await ReloadAsync(host, a.Id)).Status);
        Assert.Equal(PurchaseOrderStatus.Rejected, (await ReloadAsync(host, b.Id)).Status);
        Assert.Equal(PurchaseOrderStatus.PendingApproval, (await ReloadAsync(host, c.Id)).Status);
    }

    [Fact]
    public async Task Sweep_ShouldIgnoreSupersededWorkflows_CancelledStatusesAndMissingViews()
    {
        using var host = new PurchasingTestHost();
        var superseded = await PurchasingSeed.PendingAsync(host, "wf-new");
        var cancelled = await PurchasingSeed.PendingAsync(host, "wf-x");
        var missing = await PurchasingSeed.PendingAsync(host, "wf-y");
        var approval = MakeSweepApproval(
            (superseded.Id, "wf-old", ApprovalStatus.Approved),
            (cancelled.Id, "wf-x", ApprovalStatus.Cancelled));

        var changed = await SweepAsync(host, approval);

        Assert.Equal(0, changed);
        Assert.All(
            new[] { superseded.Id, cancelled.Id, missing.Id },
            id => Assert.Equal(PurchaseOrderStatus.PendingApproval, ReloadAsync(host, id).Result.Status));
    }

    [Fact]
    public async Task Sweep_ShouldOnlyConsiderPendingOrdersWithAWorkflow_AndSkipTheApprovalCallWhenNone()
    {
        using var host = new PurchasingTestHost();
        await PurchasingSeed.DraftAsync(host);
        var approval = MakeSweepApproval();

        var changed = await SweepAsync(host, approval);

        Assert.Equal(0, changed);
        approval.Verify(
            x => x.GetStatusesAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Sweep_ShouldTakeTheOldestPendingOrdersFirst_UpToTheBatchSize()
    {
        using var host = new PurchasingTestHost();
        var oldest = await PurchasingSeed.PendingAsync(host, "wf-1");
        host.DateTime.UtcNow = host.DateTime.UtcNow.AddMinutes(5);
        var newer = await PurchasingSeed.PendingAsync(host, "wf-2");
        var approval = MakeSweepApproval(
            (oldest.Id, "wf-1", ApprovalStatus.Approved),
            (newer.Id, "wf-2", ApprovalStatus.Approved));

        var changed = await SweepAsync(host, approval, batchSize: 1);

        Assert.Equal(1, changed);
        Assert.Equal(PurchaseOrderStatus.Approved, (await ReloadAsync(host, oldest.Id)).Status);
        Assert.Equal(PurchaseOrderStatus.PendingApproval, (await ReloadAsync(host, newer.Id)).Status);
    }

    [Theory]
    [InlineData(0, 1, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 1, true)]
    public void SweepOptions_ShouldValidateRanges(
        int interval,
        int batch,
        bool expected)
    {
        var options = new PurchaseOrderApprovalReconciliationOptions { IntervalMinutes = interval, BatchSize = batch };

        Assert.Equal(
            expected,
            System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
                options,
                new System.ComponentModel.DataAnnotations.ValidationContext(options),
                null,
                true));
    }
}
