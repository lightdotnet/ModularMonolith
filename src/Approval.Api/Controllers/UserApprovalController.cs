using Microsoft.AspNetCore.Mvc;
using StarterKit.Approval.Api.Application.Approvals.Commands;
using StarterKit.Approval.Api.Application.Approvals.Queries;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Shared;
using StarterKit.Shared.Extensions;

namespace StarterKit.Approval.Api.Controllers;

/// <summary>
/// Self-service surface, scoped to the current user — no permission gate beyond being
/// authenticated, mirroring <c>UserNotificationController</c>. Every query and command here is
/// restricted server-side to requests the current user is related to (as requester or approver);
/// see <see cref="ApprovalController"/> for the unrestricted admin surface.
/// </summary>
[ApiExplorerSettings(GroupName = "approval")]
[Route("api/v{version:apiVersion}/approval/user")]
public class UserApprovalController(ICurrentUser currentUser) : VersionedApiController
{
    private readonly string _currentUserId = currentUser.UserId
        ?? throw new ArgumentNullException(nameof(currentUser.UserId));

    /// <summary>
    /// Requests the current user is related to — as requester or as an approver on any step —
    /// filterable via <see cref="MyApprovalRequestSearchRequest.Relation"/> (requested by me,
    /// awaiting my decision, decided by me, or all of the above).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchAsync([FromQuery] MyApprovalRequestSearchRequest request)
    {
        return Ok(await Mediator.Send(new SearchMyApprovalsQuery(_currentUserId, request)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new GetMyApprovalRequestByIdQuery(id, _currentUserId)));
    }

    [HttpPut("{id}/decide")]
    public async Task<IActionResult> DecideAsync([FromRoute] string id, [FromBody] DecideApprovalRequest request)
    {
        return Ok(await Mediator.Send(
            new DecideApprovalStepCommand(id, _currentUserId, request.Approved, request.Comment)));
    }

    /// <summary>
    /// Creates an approval request as the current user. Both
    /// <see cref="CreateApprovalRequest.RequesterUserId"/> and
    /// <see cref="CreateApprovalRequest.RequesterEmployeeId"/> are resolved server-side (from the
    /// current user id and the caller's <c>employee_id</c> claim), regardless of what the caller
    /// sends, so a user can't create a request on someone else's behalf through this endpoint.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] CreateApprovalRequest request)
    {
        var scopedRequest = request with
        {
            RequesterUserId = _currentUserId,
            // Opaque bookkeeping field; null when the user has no linked employee record.
            RequesterEmployeeId = User.GetEmployeeId(),
            // RequesterName is a cosmetic label (the enforced identity is RequesterUserId above);
            // the JWT carries no name claim, so keep the client-supplied value - same pattern as
            // Notifications' caller-supplied `fromName`.
        };
        return Ok(await Mediator.Send(new CreateApprovalRequestCommand(scopedRequest)));
    }
}
