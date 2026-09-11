using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Organization.Api.Application.EmployeeLevels.Commands;
using StarterKit.Organization.Api.Application.EmployeeLevels.Queries;
using StarterKit.Organization.Contracts.Authorization;

namespace StarterKit.Organization.Api.Controllers;

[ApiExplorerSettings(GroupName = "organization")]
[Route("api/v{version:apiVersion}/employee_level")]
[MustHavePermission(OrganizationPermissions.EmployeeLevels.View)]
public class EmployeeLevelController : VersionedApiController
{
    [HttpGet("company/{companyId}")]
    public async Task<IActionResult> GetAsync([FromRoute] string companyId)
    {
        return Ok(await Mediator.Send(new GetEmployeeLevelsQuery(companyId)));
    }

    [HttpPost]
    [MustHavePermission(OrganizationPermissions.EmployeeLevels.Create)]
    public async Task<IActionResult> PostAsync([FromBody] CreateEmployeeLevelRequest request)
    {
        return Ok(await Mediator.Send(new CreateEmployeeLevelCommand(request)));
    }

    [HttpPut("{id}")]
    [MustHavePermission(OrganizationPermissions.EmployeeLevels.Update)]
    public async Task<IActionResult> PutAsync([FromRoute] string id, [FromBody] EmployeeLevelDto request)
    {
        if (id != request.Id)
            return Ok(Result.Error("Employee level ID does not match"));

        return Ok(await Mediator.Send(new UpdateEmployeeLevelCommand(request)));
    }

    [HttpDelete("{id}")]
    [MustHavePermission(OrganizationPermissions.EmployeeLevels.Delete)]
    public async Task<IActionResult> DeleteAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new DeleteEmployeeLevelCommand(id)));
    }
}
