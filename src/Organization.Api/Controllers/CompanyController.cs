using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Organization.Api.Application.Companies.Commands;
using StarterKit.Organization.Api.Application.Companies.Queries;
using StarterKit.Organization.Contracts.Authorization;

namespace StarterKit.Organization.Api.Controllers;

[ApiExplorerSettings(GroupName = "organization")]
[MustHavePermission(OrganizationPermissions.Companies.View)]
public class CompanyController : VersionedApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] CompanySearchRequest request)
    {
        return Ok(await Mediator.Send(new SearchCompaniesQuery(request)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new GetCompanyByIdQuery(id)));
    }

    [HttpPost]
    [MustHavePermission(OrganizationPermissions.Companies.Create)]
    public async Task<IActionResult> PostAsync([FromBody] CreateCompanyRequest request)
    {
        return Ok(await Mediator.Send(new CreateCompanyCommand(request)));
    }

    [HttpPut("{id}")]
    [MustHavePermission(OrganizationPermissions.Companies.Update)]
    public async Task<IActionResult> PutAsync([FromRoute] string id, [FromBody] CompanyDto request)
    {
        if (id != request.Id)
            return Ok(Result.Error("Company ID does not match"));

        return Ok(await Mediator.Send(new UpdateCompanyCommand(request)));
    }

    [HttpDelete("{id}")]
    [MustHavePermission(OrganizationPermissions.Companies.Delete)]
    public async Task<IActionResult> DeleteAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new DeleteCompanyCommand(id)));
    }
}
