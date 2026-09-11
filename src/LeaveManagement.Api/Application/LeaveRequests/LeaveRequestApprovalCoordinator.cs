using Light.Exceptions;
using StarterKit.Approval.Contracts.Approvals;
using StarterKit.Approval.Contracts.Services;
using StarterKit.LeaveManagement.Api.Data;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.Organization.Contracts.Services;

namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests;

/// <summary>
/// The one shared seam for the cross-row / cross-module orchestration behind a leave request:
/// reconciling a stale local status against Approval, the overlapping-period guard, building the
/// Approval create payload, and resolving/validating the chosen approver. Command handlers and the
/// reconciliation sweep call in here instead of duplicating the raw <see cref="IApprovalService"/>
/// / <see cref="IOrgDirectoryService"/> choreography. Cross-context ordering and compensation stay
/// in the command handlers themselves.
/// </summary>
internal sealed class LeaveRequestApprovalCoordinator(
    LeaveManagementDbContext context,
    IApprovalService approvalService,
    IOrgDirectoryService orgDirectoryService)
{
    /// <summary>
    /// For a <see cref="ReconcilableLeaveRequestsSpec"/>-matching row, pulls the current status from
    /// Approval and applies it via <see cref="ApplyOutcomeAsync"/>. <see cref="LeaveRequest.ApplyApprovalOutcome"/>
    /// is the sole authority on whether the returned view belongs to a superseded workflow or an
    /// already-finalized row — this method does not pre-check that itself.
    /// </summary>
    public async Task ReconcileStatusAsync(
        LeaveRequest entity,
        CancellationToken cancellationToken)
    {
        if (!new ReconcilableLeaveRequestsSpec().IsSatisfiedBy(entity))
            return;

        var view = await approvalService.GetStatusByRequestAsync(
            LeaveRequestStatusMap.RequestType,
            entity.Id,
            cancellationToken);

        if (view is null)
            return;

        await ApplyOutcomeAsync(entity, context, view, cancellationToken);
    }

    /// <summary>
    /// Fails with a <c>Result.Error</c> when the employee already has a blocking leave request over
    /// an overlapping period. Pass <paramref name="excludeRequestId"/> to skip the row being edited.
    /// </summary>
    public async Task<IResult> EnsureNoOverlapAsync(
        string employeeId,
        DateRange period,
        string? excludeRequestId,
        CancellationToken cancellationToken)
    {
        var overlaps = await context.LeaveRequests
            .Where(new OverlappingLeaveRequestsSpec(employeeId, period, excludeRequestId))
            .AnyAsync(cancellationToken);

        return overlaps
            ? Result.Error("You already have a leave request that covers an overlapping period.")
            : Result.Success();
    }

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
    /// Builds the single-step <see cref="CreateApprovalRequest"/> payload (identical shape on the
    /// create and resubmit paths) and creates the Approval workflow. <paramref name="leaveType"/> /
    /// <paramref name="reason"/> are passed explicitly because the resubmit path creates the
    /// workflow against the edited values before they are applied to <paramref name="entity"/>.
    /// </summary>
    public Task<IResult<string>> CreateApprovalAsync(
        LeaveRequest entity,
        LeaveType leaveType,
        string? reason,
        ResolvedApproverDto approver,
        string? requesterName,
        CancellationToken cancellationToken) =>
        approvalService.CreateAsync(
            new CreateApprovalRequest(
                RequestType: LeaveRequestStatusMap.RequestType,
                RequestId: entity.Id,
                RequesterUserId: entity.UserId,
                RequesterEmployeeId: entity.EmployeeId,
                RequesterName: requesterName,
                Title: $"{leaveType} leave request",
                Content: reason,
                DeepLinkUrl: $"/leave-requests/{entity.Id}",
                DocumentTypeId: null,
                ApproverChain:
                [
                    new ApproverStepInput(1, approver.UserId, approver.EmployeeId, approver.Name),
                ]),
            cancellationToken);

    /// <summary>
    /// The pure "map the status and apply it" step — no I/O — shared by every reconcile call site
    /// (this coordinator's own <see cref="ReconcileStatusAsync"/>, the periodic sweep in
    /// <c>LeaveRequestReconciliationService</c>, and the <c>ApprovalFinalizedIntegrationEventHandler</c>
    /// subscriber) instead of three near-identical copies. Returns whether the row actually changed.
    /// </summary>
    public static bool TryApplyOutcome(
        LeaveRequest entity,
        ApprovalStatusView view)
    {
        var mapped = LeaveRequestStatusMap.MapStatus(view.Status);
        return entity.ApplyApprovalOutcome(view.ApprovalRequestId, mapped);
    }

    /// <summary>
    /// <see cref="TryApplyOutcome"/> plus an immediate save when it changed — for call sites that
    /// reconcile a single row and want that write persisted right away (this coordinator's own
    /// <see cref="ReconcileStatusAsync"/>, and the <c>ApprovalFinalizedIntegrationEventHandler</c>
    /// subscriber). The periodic sweep instead calls <see cref="TryApplyOutcome"/> directly per row
    /// and batches one save after its whole loop. Taking the <see cref="LeaveManagementDbContext"/>
    /// as a parameter — rather than this coordinator's own instance — lets call sites that don't
    /// share this coordinator's DI scope/lifetime reuse it too.
    /// </summary>
    public static async Task<bool> ApplyOutcomeAsync(
        LeaveRequest entity,
        LeaveManagementDbContext context,
        ApprovalStatusView view,
        CancellationToken cancellationToken)
    {
        if (!TryApplyOutcome(entity, view))
            return false;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Runs a domain-mutating factory and translates a thrown <see cref="ExceptionBase"/> — the
    /// aggregate's sanctioned guard-failure signal — into a <see cref="Result{T}"/>, mirroring
    /// <c>ApprovalService</c>'s catch-and-translate pattern at its own aggregate boundary. Use
    /// <see cref="ToResult{T}"/> directly instead when the call must run inside a call site's own
    /// try/catch (e.g. because that catch also drives a compensating action).
    /// </summary>
    public static IResult<T> Guard<T>(Func<T> action)
    {
        try
        {
            return Result<T>.Success(action());
        }
        catch (ExceptionBase ex)
        {
            return ToResult<T>(ex);
        }
    }

    /// <summary>
    /// <see cref="Guard{T}"/> for a domain-mutating action with no return value.
    /// </summary>
    public static IResult Guard(Action action)
    {
        try
        {
            action();
            return Result.Success();
        }
        catch (ExceptionBase ex)
        {
            return ToResult(ex);
        }
    }

    /// <summary>
    /// Translates an already-caught <see cref="ExceptionBase"/> into a <see cref="Result{T}"/>.
    /// </summary>
    public static IResult<T> ToResult<T>(ExceptionBase ex) => ex switch
    {
        ConflictException => Result<T>.Conflict(ex.Message),
        ForbiddenException => Result<T>.Forbidden(ex.Message),
        _ => Result<T>.Error(DescribeValidation(ex)),
    };

    /// <summary>
    /// Translates an already-caught <see cref="ExceptionBase"/> into a <see cref="Result"/>.
    /// </summary>
    public static IResult ToResult(ExceptionBase ex) => ex switch
    {
        ConflictException => Result.Conflict(ex.Message),
        ForbiddenException => Result.Forbidden(ex.Message),
        _ => Result.Error(DescribeValidation(ex)),
    };

    private static string DescribeValidation(ExceptionBase ex) =>
        ex is ValidationException v && v.ValidationErrors.Count > 0
            ? string.Join("|", v.ValidationErrors.Select(e => $"{e.Key}: {string.Join(",", e.Value)}"))
            : ex.Message;
}
