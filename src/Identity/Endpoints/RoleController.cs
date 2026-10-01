using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Modules.Identity.Application.Common.Models;
using StarterKit.Modules.Identity.Application.Roles.Commands;
using StarterKit.Modules.Identity.Application.Roles.Services;

namespace StarterKit.Modules.Identity.Endpoints;

[ApiExplorerSettings(GroupName = "identity")]
[MustHavePermission(IdentityPermissions.Roles.View)]
public class RoleController(IRoleService roleService) : VersionedApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        return Ok(await roleService.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync([FromRoute] string id)
    {
        return Ok(await roleService.GetByIdAsync(id));
    }

    [HttpPost]
    [Authorize(Policy = IdentityPermissions.Roles.Manage)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateRoleRequest request)
    {
        return Ok(await Mediator.Send(new CreateRoleCommand(request)));
    }

    [HttpPut]
    [Authorize(Policy = IdentityPermissions.Roles.Manage)]
    public async Task<IActionResult> UpdateAsync([FromBody] RoleDto request)
    {
        return Ok(await Mediator.Send(new UpdateRoleCommand(request)));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = IdentityPermissions.Roles.Manage)]
    public async Task<IActionResult> DeleteAsync([FromRoute] string id)
    {
        return Ok(await Mediator.Send(new DeleteRoleCommand(id)));
    }
}
