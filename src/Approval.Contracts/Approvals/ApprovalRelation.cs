namespace StarterKit.Approval.Contracts.Approvals;

/// <summary>
/// How the current user relates to an approval request, used to filter the
/// self-service "my approvals" search to only what's relevant to them.
/// </summary>
public enum ApprovalRelation
{
    /// <summary>Requester, or an approver on any step — every request touching the current user.</summary>
    All,

    /// <summary>Requests the current user created.</summary>
    Requested,

    /// <summary>Requests currently sitting at a step assigned to the current user, awaiting their decision.</summary>
    AwaitingMyDecision,

    /// <summary>Requests where the current user has already decided a step (approved or rejected it).</summary>
    DecidedByMe,
}
