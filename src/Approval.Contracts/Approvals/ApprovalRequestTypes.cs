namespace StarterKit.Approval.Contracts.Approvals;

/// <summary>
/// Request types owned by another module. A reserved type may only be created in-process by its
/// owning module through <c>IApprovalService.CreateAsync</c>; the HTTP surfaces of Approval reject
/// it, so a caller cannot forge a request that shadows a genuine workflow or spoofs an approver's
/// inbox. A module that starts driving its own workflow through Approval adds its type here.
/// </summary>
public static class ApprovalRequestTypes
{
    public const string LeaveRequest = "LeaveRequest";

    public const string PurchaseOrder = "PurchaseOrder";

    private static readonly string[] Reserved =
    [
        LeaveRequest,
        PurchaseOrder,
    ];

    /// <summary>
    /// Whether <paramref name="requestType"/> belongs to a module (case-insensitive, ignoring
    /// surrounding whitespace).
    /// </summary>
    public static bool IsReserved(string? requestType) =>
        requestType is not null
        && Reserved.Contains(requestType.Trim(), StringComparer.OrdinalIgnoreCase);
}
