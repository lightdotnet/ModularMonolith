using Light.Exceptions;
using StarterKit.Shared.Entities;

namespace StarterKit.LeaveManagement.Api.Domain.LeaveRequests;

/// <summary>
/// Aggregate root for a single employee leave request. Ownership is keyed off <see cref="UserId"/>;
/// the multi-level approval workflow is delegated to the Approval module and pointed at by the
/// opaque <see cref="ApprovalRequestId"/>. Every state transition runs through a guarded behaviour
/// method — <see cref="Create"/>, <see cref="LinkApprovalRequest"/>, <see cref="Resubmit"/>,
/// <see cref="ReviseDetails"/>, <see cref="ApplyApprovalOutcome"/> — rather than open setters.
/// </summary>
public class LeaveRequest : AuditableEntity
{
    private LeaveRequest()
    {
    }

    public string UserId { get; private set; } = null!;

    public string EmployeeId { get; private set; } = null!;

    public LeaveType LeaveType { get; private set; }

    public DateRange Period { get; private set; } = null!;

    public string? Reason { get; private set; }

    public LeaveRequestStatus Status { get; private set; } = LeaveRequestStatus.Pending;

    public string? ApprovalRequestId { get; private set; }

    /// <summary>
    /// Whether the owning employee may still edit or withdraw this request. Handlers translate this
    /// to their own <c>Result.Error</c> text — the domain carries no Result/HTTP concern.
    /// </summary>
    public bool IsOwnerActionable =>
        Status is LeaveRequestStatus.Pending or LeaveRequestStatus.Rejected;

    /// <summary>
    /// Builds a new, still-unlinked leave request in <see cref="LeaveRequestStatus.Pending"/>. The
    /// approval workflow is attached separately via <see cref="LinkApprovalRequest"/> once it has
    /// been created. <see cref="AuditableEntity"/> assigns <c>Id</c> at construction.
    /// </summary>
    public static LeaveRequest Create(
        string userId,
        string employeeId,
        LeaveType leaveType,
        DateRange period,
        string? reason)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw Invalid(nameof(userId), "A requester user is required.");

        if (string.IsNullOrWhiteSpace(employeeId))
            throw Invalid(nameof(employeeId), "A requester employee is required.");

        // A null DateRange is a caller-programming-error, not a user-facing validation failure —
        // every caller in this module always constructs the DateRange first (see DateRange's own
        // guard against an invalid range), so ArgumentNullException fits better here than the
        // ValidationException used above for the blank-string guards.
        ArgumentNullException.ThrowIfNull(period);

        return new LeaveRequest
        {
            UserId = userId,
            EmployeeId = employeeId,
            LeaveType = leaveType,
            Period = period,
            Reason = reason,
            ApprovalRequestId = null,
        };
    }

    /// <summary>
    /// Attaches the freshly-created Approval workflow. Valid only once, while the request is still
    /// <see cref="LeaveRequestStatus.Pending"/> and unlinked.
    /// </summary>
    public void LinkApprovalRequest(string approvalRequestId)
    {
        if (string.IsNullOrWhiteSpace(approvalRequestId))
            throw Invalid(nameof(approvalRequestId), "An approval request id is required.");

        if (Status != LeaveRequestStatus.Pending || ApprovalRequestId is not null)
            throw new ConflictException("This leave request is already linked to an approval workflow.");

        ApprovalRequestId = approvalRequestId;
    }

    /// <summary>
    /// Owner re-submission: swaps in a brand-new approval workflow and resets the local status to
    /// <see cref="LeaveRequestStatus.Pending"/> against the edited fields. This is the only place
    /// the approval-id swap and the status reset are coupled.
    /// </summary>
    public void Resubmit(
        LeaveType leaveType,
        DateRange period,
        string? reason,
        string newApprovalRequestId)
    {
        ArgumentNullException.ThrowIfNull(period);

        if (string.IsNullOrWhiteSpace(newApprovalRequestId))
            throw Invalid(nameof(newApprovalRequestId), "An approval request id is required.");

        if (!IsOwnerActionable)
            throw new ConflictException("This leave request can no longer be resubmitted.");

        LeaveType = leaveType;
        Period.Update(period.Start, period.End);
        Reason = reason;
        ApprovalRequestId = newApprovalRequestId;
        Status = LeaveRequestStatus.Pending;
    }

    /// <summary>
    /// Manage-only metadata correction. Applies the edited fields without ever touching
    /// <see cref="Status"/> or <see cref="ApprovalRequestId"/>, and enforces no status guard.
    /// </summary>
    public void ReviseDetails(
        LeaveType leaveType,
        DateRange period,
        string? reason)
    {
        ArgumentNullException.ThrowIfNull(period);

        LeaveType = leaveType;
        Period.Update(period.Start, period.End);
        Reason = reason;
    }

    /// <summary>
    /// The single reconcile choke point. Applies a terminal approval outcome to the local status,
    /// returning <c>false</c> (no-op) when the outcome belongs to a superseded workflow
    /// (<paramref name="approvalRequestId"/> mismatch), the request is already finalized
    /// (<see cref="Status"/> is not <see cref="LeaveRequestStatus.Pending"/> — never un-finalize),
    /// or the outcome equals the current status.
    /// </summary>
    public bool ApplyApprovalOutcome(
        string approvalRequestId,
        LeaveRequestStatus outcome)
    {
        if (approvalRequestId != ApprovalRequestId)
            return false;

        if (Status != LeaveRequestStatus.Pending)
            return false;

        if (outcome == Status)
            return false;

        Status = outcome;
        return true;
    }

    public bool IsOwnedBy(string userId) => UserId == userId;

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
