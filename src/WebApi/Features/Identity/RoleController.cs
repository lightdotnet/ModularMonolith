using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolith.Authorization;
using Monolith.Identity;

namespace Monolith.Features.Identity;

[MustHavePermission(Permissions.Roles.View)]
public class RoleController(IRoleService roleService) : ApiControllerBase
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
    [Authorize(Policy = Permissions.Roles.Manage)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateRoleRequest request)
    {
        return Ok(await roleService.CreateAsync(request));
    }

    [HttpPut]
    [Authorize(Policy = Permissions.Roles.Manage)]
    public async Task<IActionResult> UpdateAsync([FromBody] RoleDto request)
    {
        return Ok(await roleService.UpdateAsync(request));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.Roles.Manage)]
    public async Task<IActionResult> DeleteAsync([FromRoute] string id)
    {
        return Ok(await roleService.DeleteAsync(id));
    }
}
