using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Organization.Api.Application.OrgUnits.Commands;
using StarterKit.Organization.Api.Application.OrgUnits.Queries;
using StarterKit.Organization.Contracts.Authorization;

namespace StarterKit.Organization.Api.Controllers;

[ApiExplorerSettings(GroupName = "organization")]
[Route("api/v{version:apiVersion}/org_unit")]
[MustHavePermission(OrganizationPermissions.OrgUnits.View)]
public class OrgUnitController : VersionedApiController
{
    [HttpGet("company/{companyId}/tree")]
    public async Task<IActionResult> GetTreeAsync([FromRoute] string companyId)
    {
        return Ok(await Mediator.Send(new GetOrgUnitTreeQuery(companyId)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new GetOrgUnitByIdQuery(id)));
    }

    [HttpGet("{id}/employee")]
    public async Task<IActionResult> GetEmployeesAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new GetOrgUnitEmployeesQuery(id)));
    }

    [HttpGet("{id}/manager")]
    public async Task<IActionResult> GetManagersAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new GetOrgUnitManagersQuery(id)));
    }

    [HttpPost]
    [MustHavePermission(OrganizationPermissions.OrgUnits.Create)]
    public async Task<IActionResult> PostAsync([FromBody] CreateOrgUnitRequest request)
    {
        return Ok(await Mediator.Send(new CreateOrgUnitCommand(request)));
    }

    [HttpPut("{id}")]
    [MustHavePermission(OrganizationPermissions.OrgUnits.Update)]
    public async Task<IActionResult> PutAsync([FromRoute] string id, [FromBody] OrgUnitDto request)
    {
        if (id != request.Id)
            return Ok(Result.Error("Org unit ID does not match"));

        return Ok(await Mediator.Send(new UpdateOrgUnitCommand(request)));
    }

    [HttpPut("{id}/move")]
    [MustHavePermission(OrganizationPermissions.OrgUnits.Update)]
    public async Task<IActionResult> MoveAsync([FromRoute] string id, [FromBody] MoveOrgUnitRequest request)
    {
        return Ok(await Mediator.Send(new MoveOrgUnitCommand(id, request)));
    }

    [HttpDelete("{id}")]
    [MustHavePermission(OrganizationPermissions.OrgUnits.Delete)]
    public async Task<IActionResult> DeleteAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new DeleteOrgUnitCommand(id)));
    }
}
