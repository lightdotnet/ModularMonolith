using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Locations.Api.Application.Locations.Commands;
using StarterKit.Locations.Api.Application.Locations.Queries;
using StarterKit.Locations.Contracts.Authorization;

namespace StarterKit.Locations.Api.Controllers;

[ApiExplorerSettings(GroupName = "location")]
[Route("api/v{version:apiVersion}/location")]
[MustHavePermission(LocationPermissions.Locations.View)]
public class LocationController : VersionedApiController
{
    [HttpGet("tree")]
    public async Task<IActionResult> GetTreeAsync()
    {
        return Ok(await Mediator.Send(new GetLocationTreeQuery()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new GetLocationByIdQuery(id)));
    }

    [HttpGet("{id}/children")]
    public async Task<IActionResult> GetChildrenAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new GetLocationChildrenQuery(id)));
    }

    [HttpPost]
    [MustHavePermission(LocationPermissions.Locations.Manage)]
    public async Task<IActionResult> PostAsync([FromBody] CreateLocationRequest request)
    {
        return Ok(await Mediator.Send(new CreateLocationCommand(request)));
    }

    [HttpPut("{id}")]
    [MustHavePermission(LocationPermissions.Locations.Manage)]
    public async Task<IActionResult> PutAsync([FromRoute] string id, [FromBody] UpdateLocationRequest request)
    {
        return Ok(await Mediator.Send(new UpdateLocationCommand(id, request)));
    }

    [HttpPut("{id}/move")]
    [MustHavePermission(LocationPermissions.Locations.Manage)]
    public async Task<IActionResult> MoveAsync([FromRoute] string id, [FromBody] MoveLocationRequest request)
    {
        return Ok(await Mediator.Send(new MoveLocationCommand(id, request)));
    }

    [HttpDelete("{id}")]
    [MustHavePermission(LocationPermissions.Locations.Manage)]
    public async Task<IActionResult> DeleteAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new DeleteLocationCommand(id)));
    }
}
