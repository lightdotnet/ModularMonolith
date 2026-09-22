using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Locations.Api.Application.LocationTypes.Commands;
using StarterKit.Locations.Api.Application.LocationTypes.Queries;
using StarterKit.Locations.Contracts.Authorization;

namespace StarterKit.Locations.Api.Controllers;

[ApiExplorerSettings(GroupName = "location")]
[Route("api/v{version:apiVersion}/location_type")]
[MustHavePermission(LocationPermissions.LocationTypes.View)]
public class LocationTypeController : VersionedApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        return Ok(await Mediator.Send(new GetLocationTypesQuery()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new GetLocationTypeByIdQuery(id)));
    }

    [HttpPost]
    [MustHavePermission(LocationPermissions.LocationTypes.Manage)]
    public async Task<IActionResult> PostAsync([FromBody] CreateLocationTypeRequest request)
    {
        return Ok(await Mediator.Send(new CreateLocationTypeCommand(request)));
    }

    [HttpPut("{id}")]
    [MustHavePermission(LocationPermissions.LocationTypes.Manage)]
    public async Task<IActionResult> PutAsync([FromRoute] string id, [FromBody] UpdateLocationTypeRequest request)
    {
        return Ok(await Mediator.Send(new UpdateLocationTypeCommand(id, request)));
    }

    [HttpDelete("{id}")]
    [MustHavePermission(LocationPermissions.LocationTypes.Manage)]
    public async Task<IActionResult> DeleteAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new DeleteLocationTypeCommand(id)));
    }
}
