using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests.Commands;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests.Queries;
using StarterKit.LeaveManagement.Contracts.Authorization;
using StarterKit.Shared;
using StarterKit.Shared.Extensions;

namespace StarterKit.LeaveManagement.Api.Controllers;

/// <summary>
/// Self-service surface, scoped to the current user — no permission gate beyond being
/// authenticated, mirroring <c>UserApprovalController</c>. Every query and command here is
/// restricted server-side to the caller's own leave requests unless they hold
/// <see cref="LeaveManagementPermissions.Requests.Manage"/>.
/// </summary>
[ApiExplorerSettings(GroupName = "leave_management")]
[Route("api/v{version:apiVersion}/leave_request")]
public class LeaveRequestController(ICurrentUser currentUser) : VersionedApiController
{
    private readonly string _currentUserId = currentUser.UserId
        ?? throw new ArgumentNullException(nameof(currentUser.UserId));

    private bool CanManage => currentUser.HasPermission(LeaveManagementPermissions.Requests.Manage);

    [HttpGet("search")]
    public async Task<IActionResult> SearchAsync([FromQuery] LeaveRequestSearchRequest request)
    {
        return Ok(await Mediator.Send(
            new SearchLeaveRequestsQuery(request, _currentUserId, CanManage)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(
            new GetLeaveRequestByIdQuery(id, _currentUserId, CanManage)));
    }

    [HttpGet("approvers")]
    public async Task<IActionResult> GetApproversAsync()
    {
        return Ok(await Mediator.Send(new GetLeaveRequestApproverCandidatesQuery(User.GetEmployeeId())));
    }

    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] CreateLeaveRequest request)
    {
        return Ok(await Mediator.Send(new CreateLeaveRequestCommand(
            request,
            _currentUserId,
            User.GetEmployeeId())));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutAsync([FromRoute] string id, [FromBody] UpdateLeaveRequest request)
    {
        return Ok(await Mediator.Send(new UpdateLeaveRequestCommand(
            id,
            request,
            _currentUserId,
            CanManage)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new DeleteLeaveRequestCommand(id, _currentUserId, CanManage)));
    }
}
