using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Approval.Api.Application.DocumentTypes.Commands;
using StarterKit.Approval.Api.Application.DocumentTypes.Queries;
using StarterKit.Approval.Contracts.Authorization;
using StarterKit.Approval.Contracts.DocumentTypes;
using StarterKit.Infrastructure.Endpoints;

namespace StarterKit.Approval.Api.Controllers;

/// <summary>
/// Admin-managed catalog of approval document types — a small reference list a request can
/// optionally be tagged with. The list is readable by any authenticated user (it feeds the
/// create-request document-type picker); the single-record read and every write are
/// permission-gated.
/// </summary>
[ApiExplorerSettings(GroupName = "approval")]
[Route("api/v{version:apiVersion}/approval/document_type")]
public class ApprovalDocumentTypeController : VersionedApiController
{
    [HttpGet]
    public async Task<IActionResult> GetListAsync([FromQuery] bool? activeOnly)
    {
        return Ok(await Mediator.Send(new GetApprovalDocumentTypesQuery(activeOnly)));
    }

    [HttpGet("{id}")]
    [MustHavePermission(ApprovalPermissions.DocumentTypes.View)]
    public async Task<IActionResult> GetByIdAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new GetApprovalDocumentTypeByIdQuery(id)));
    }

    [HttpPost]
    [MustHavePermission(ApprovalPermissions.DocumentTypes.Create)]
    public async Task<IActionResult> PostAsync([FromBody] CreateApprovalDocumentTypeRequest request)
    {
        return Ok(await Mediator.Send(new CreateApprovalDocumentTypeCommand(request)));
    }

    [HttpPut("{id}")]
    [MustHavePermission(ApprovalPermissions.DocumentTypes.Update)]
    public async Task<IActionResult> PutAsync([FromRoute] string id, [FromBody] ApprovalDocumentTypeDto request)
    {
        if (id != request.Id)
            return Ok(Result.Error("Document type ID does not match"));

        return Ok(await Mediator.Send(new UpdateApprovalDocumentTypeCommand(request)));
    }

    [HttpDelete("{id}")]
    [MustHavePermission(ApprovalPermissions.DocumentTypes.Delete)]
    public async Task<IActionResult> DeleteAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new DeleteApprovalDocumentTypeCommand(id)));
    }
}
