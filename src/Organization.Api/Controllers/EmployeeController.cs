using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Organization.Api.Application.Employees.Commands;
using StarterKit.Organization.Api.Application.Employees.Queries;
using StarterKit.Organization.Contracts.Authorization;

namespace StarterKit.Organization.Api.Controllers;

[ApiExplorerSettings(GroupName = "organization")]
public class EmployeeController : VersionedApiController
{
    // Company-wide employee directory lookup: any authenticated user may search for basic
    // contact details. The projection in SearchEmployeesQuery omits sensitive PII.
    [HttpGet("search")]
    public async Task<IActionResult> SearchAsync([FromQuery] EmployeeSearchRequest request)
    {
        return Ok(await Mediator.Send(new SearchEmployeesQuery(request)));
    }

    [HttpGet("{id}")]
    [MustHavePermission(OrganizationPermissions.Employees.View)]
    public async Task<IActionResult> GetAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new GetEmployeeByIdQuery(id)));
    }

    [HttpPost]
    [MustHavePermission(OrganizationPermissions.Employees.Create)]
    public async Task<IActionResult> PostAsync([FromBody] CreateEmployeeRequest request)
    {
        return Ok(await Mediator.Send(new CreateEmployeeCommand(request)));
    }

    [HttpPut("{id}")]
    [MustHavePermission(OrganizationPermissions.Employees.Update)]
    public async Task<IActionResult> PutAsync([FromRoute] string id, [FromBody] EmployeeDto request)
    {
        if (id != request.Id)
            return Ok(Result.Error("Employee ID does not match"));

        return Ok(await Mediator.Send(new UpdateEmployeeCommand(request)));
    }

    [HttpDelete("{id}")]
    [MustHavePermission(OrganizationPermissions.Employees.Delete)]
    public async Task<IActionResult> DeleteAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new DeleteEmployeeCommand(id)));
    }

    [HttpPost("{id}/org_unit")]
    [MustHavePermission(OrganizationPermissions.Employees.Update)]
    public async Task<IActionResult> AssignOrgUnitAsync(
        [FromRoute] string id, [FromBody] AssignEmployeeOrgUnitRequest request)
    {
        return Ok(await Mediator.Send(new AssignEmployeeToOrgUnitCommand(id, request)));
    }

    [HttpPut("{id}/org_unit/{orgUnitId}")]
    [MustHavePermission(OrganizationPermissions.Employees.Update)]
    public async Task<IActionResult> UpdateMembershipAsync(
        [FromRoute] string id, [FromRoute] string orgUnitId, [FromBody] UpdateEmployeeMembershipRequest request)
    {
        return Ok(await Mediator.Send(new UpdateEmployeeMembershipCommand(id, orgUnitId, request)));
    }

    [HttpDelete("{id}/org_unit/{orgUnitId}")]
    [MustHavePermission(OrganizationPermissions.Employees.Update)]
    public async Task<IActionResult> RemoveFromOrgUnitAsync(
        [FromRoute] string id, [FromRoute] string orgUnitId)
    {
        return Ok(await Mediator.Send(new RemoveEmployeeFromOrgUnitCommand(id, orgUnitId)));
    }

    [HttpPost("{id}/login")]
    [MustHavePermission(OrganizationPermissions.Employees.ManageLogin)]
    public async Task<IActionResult> CreateLoginAsync(
        [FromRoute] string id, [FromBody] CreateEmployeeLoginRequest request)
    {
        return Ok(await Mediator.Send(new CreateEmployeeLoginCommand(id, request)));
    }

    [HttpPut("{id}/login")]
    [MustHavePermission(OrganizationPermissions.Employees.ManageLogin)]
    public async Task<IActionResult> LinkLoginAsync(
        [FromRoute] string id, [FromBody] LinkEmployeeLoginRequest request)
    {
        return Ok(await Mediator.Send(new LinkEmployeeLoginCommand(id, request)));
    }

    [HttpDelete("{id}/login")]
    [MustHavePermission(OrganizationPermissions.Employees.ManageLogin)]
    public async Task<IActionResult> UnlinkLoginAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new UnlinkEmployeeLoginCommand(id)));
    }
}
