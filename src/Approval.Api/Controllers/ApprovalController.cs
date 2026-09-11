using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Approval.Api.Application.Approvals.Commands;
using StarterKit.Approval.Api.Application.Approvals.Queries;
using StarterKit.Approval.Contracts.Authorization;
using StarterKit.Infrastructure.Endpoints;

namespace StarterKit.Approval.Api.Controllers;

/// <summary>
/// Admin/back-office surface — unrestricted visibility across every request, gated by
/// <see cref="ApprovalPermissions.Requests.ViewAll"/>. Self-service actions (a user's own
/// requests, deciding a step assigned to them) live on <see cref="UserApprovalController"/> instead.
/// </summary>
[ApiExplorerSettings(GroupName = "approval")]
[MustHavePermission(ApprovalPermissions.Requests.ViewAll)]
public class ApprovalController : VersionedApiController
{
    [HttpGet]
    public async Task<IActionResult> SearchAsync([FromQuery] ApprovalRequestSearchRequest request)
    {
        return Ok(await Mediator.Send(new SearchApprovalRequestsQuery(request)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new GetApprovalRequestByIdQuery(id)));
    }

    /// <summary>
    /// Creates an approval request directly over HTTP. Normal request types (Leave, etc.) are
    /// expected to create theirs via <see cref="IApprovalService"/> in-process instead — this
    /// endpoint exists for ad-hoc/admin-triggered requests and exercising the engine directly
    /// (e.g. picking an arbitrary requester/approver chain). A user creating a real request for
    /// themselves goes through <see cref="UserApprovalController.PostAsync"/> instead.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] CreateApprovalRequest request)
    {
        return Ok(await Mediator.Send(new CreateApprovalRequestCommand(request)));
    }
}
