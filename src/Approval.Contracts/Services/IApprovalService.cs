using StarterKit.Approval.Contracts.Approvals;

namespace StarterKit.Approval.Contracts.Services;

/// <summary>
/// The seam other modules use to plug into the generic approval engine — the only way
/// Approval is meant to be consumed cross-module (mirrors <c>IUserService</c>/<c>INotificationService</c>).
/// </summary>
public interface IApprovalService
{
    /// <summary>
    /// Creates a new approval request with an already-resolved approver chain and notifies
    /// the first level's approver.
    /// </summary>
    Task<IResult<string>> CreateAsync(CreateApprovalRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a decision for the current level by <paramref name="decidedByUserId"/>. Advances
    /// to the next level (and notifies its approver) on approval, or finalizes the request as
    /// rejected. Notifies the original requester once the request reaches a final state.
    /// </summary>
    Task<IResult> DecideAsync(
        string approvalRequestId,
        string decidedByUserId,
        bool approved,
        string? comment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a still-pending request — used when the source record (e.g. a leave request)
    /// is deleted or withdrawn before it's been decided. Only the original requester
    /// (<paramref name="cancelledByUserId"/> == the request's requester) may cancel.
    /// </summary>
    Task<IResult> CancelAsync(
        string approvalRequestId,
        string cancelledByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lean status lookup by the exact stored approval request id — the id the owning module keeps on
    /// its own record. Unlike the (type, id) lookups this cannot be shadowed by another request that
    /// merely names the same source record. <c>null</c> when no such request exists.
    /// </summary>
    Task<ApprovalStatusView?> GetStatusAsync(
        string approvalRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batched lookup by stored approval request id, keyed by that id. Unknown ids are absent.
    /// </summary>
    Task<IReadOnlyDictionary<string, ApprovalStatusView>> GetStatusesAsync(
        IReadOnlyCollection<string> approvalRequestIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lean status lookup for a single source record — the most recent approval request tied to
    /// <paramref name="requestType"/>/<paramref name="requestId"/>, or <c>null</c> when none exists.
    /// </summary>
    Task<ApprovalStatusView?> GetStatusByRequestAsync(
        string requestType,
        string requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batched lean status lookup — the most recent approval request for each of
    /// <paramref name="requestIds"/> under <paramref name="requestType"/>, keyed by request id.
    /// Ids with no approval request are absent from the result.
    /// </summary>
    Task<IReadOnlyDictionary<string, ApprovalStatusView>> GetStatusesByRequestAsync(
        string requestType,
        IReadOnlyCollection<string> requestIds,
        CancellationToken cancellationToken = default);
}
