using StarterKit.Approval.Contracts.Approvals;
using StarterKit.Approval.Contracts.Services;
using StarterKit.Organization.Contracts.Services;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders;

/// <summary>
/// The one shared seam for the cross-module orchestration behind a purchase order approval: validating
/// the chosen approver against Organization, building the Approval create payload, and applying an
/// approval outcome to the local order. Command handlers, the <c>ApprovalFinalizedIntegrationEvent</c>
/// subscriber and the reconciliation sweep call in here instead of duplicating the raw
/// <see cref="IApprovalService"/> / <see cref="IOrgDirectoryService"/> choreography. Cross-context
/// ordering and compensation stay in the command handlers themselves. Mirrors LeaveManagement's
/// <c>LeaveRequestApprovalCoordinator</c>.
/// </summary>
internal sealed class PurchaseOrderApprovalCoordinator(
    IApprovalService approvalService,
    IOrgDirectoryService orgDirectoryService)
{
    /// <summary>
    /// Resolves the eligible approver candidates for <paramref name="employeeId"/> and validates the
    /// caller's chosen <paramref name="chosenApproverEmployeeId"/> against that list.
    /// </summary>
    public async Task<IResult<ResolvedApproverDto>> ResolveApproverAsync(
        string employeeId,
        string chosenApproverEmployeeId,
        CancellationToken cancellationToken)
    {
        var candidates = await orgDirectoryService.GetApproverCandidatesAsync(
            employeeId,
            cancellationToken);

        if (candidates.Count == 0)
            return Result<ResolvedApproverDto>.Error(
                "No approver could be determined for this employee's department.");

        var approver = candidates.FirstOrDefault(x => x.EmployeeId == chosenApproverEmployeeId);

        return approver is null
            ? Result<ResolvedApproverDto>.Error("Invalid approver selection.")
            : Result<ResolvedApproverDto>.Success(approver);
    }

    /// <summary>
    /// Builds the single-step <see cref="CreateApprovalRequest"/> payload (identical on the submit and
    /// resubmit paths) and creates the Approval workflow. The requester name is resolved server-side from
    /// Organization, and both display labels are snapshots (known-debt D5).
    /// </summary>
    public async Task<IResult<string>> CreateApprovalAsync(
        PurchaseOrder order,
        ResolvedApproverDto approver,
        CancellationToken cancellationToken)
    {
        var requesterName = await orgDirectoryService.GetEmployeeNameAsync(
            order.RequesterEmployeeId,
            cancellationToken);

        var total = order.TotalAmount;

        return await approvalService.CreateAsync(
            new CreateApprovalRequest(
                RequestType: PurchaseOrderStatusMap.RequestType,
                RequestId: order.Id.ToString(),
                RequesterUserId: order.RequesterUserId,
                RequesterEmployeeId: order.RequesterEmployeeId,
                RequesterName: requesterName,
                Title: $"Purchase order {order.PONumber}",
                Content: $"{order.SupplierName} | {order.Lines.Count} line(s) | total {total.Amount:N2} {total.Currency}",
                DeepLinkUrl: $"/purchasing/purchase-orders/{order.Id}",
                DocumentTypeId: null,
                ApproverChain:
                [
                    new ApproverStepInput(1, approver.UserId, approver.EmployeeId, approver.Name),
                ]),
            cancellationToken);
    }

    /// <summary>
    /// The pure "map the status and apply it" step — no I/O — shared by every reconcile call site (the
    /// integration-event subscriber and the periodic sweep). <see cref="PurchaseOrder.ApplyApprovalOutcome"/>
    /// is the sole authority on ignoring a superseded workflow or an already-decided order. Returns
    /// whether the order actually changed.
    /// </summary>
    public static bool TryApplyOutcome(
        PurchaseOrder order,
        ApprovalStatusView view,
        DateTimeOffset now)
    {
        if (!PurchaseOrderStatusMap.TryMapDecision(view.Status, out var approved))
            return false;

        return order.ApplyApprovalOutcome(view.ApprovalRequestId, approved, now);
    }

    /// <summary>
    /// <see cref="TryApplyOutcome"/> plus an immediate save when it changed — for the single-row
    /// call site (the integration-event subscriber). The periodic sweep instead calls
    /// <see cref="TryApplyOutcome"/> per row and batches one save after its whole loop.
    /// </summary>
    public static async Task<bool> ApplyOutcomeAsync(
        PurchaseOrder order,
        PurchasingDbContext context,
        ApprovalStatusView view,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!TryApplyOutcome(order, view, now))
            return false;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
