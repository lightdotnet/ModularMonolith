using Microsoft.AspNetCore.Mvc;
using StarterKit.Infrastructure.Endpoints;

namespace StarterKit.Identity.Api.Controllers;

[ApiExplorerSettings(GroupName = "identity")]
public class PermissionsController(
    IPermissionManager permissionManager) : VersionedApiController
{
    [HttpGet]
    public IActionResult GetAsync()
    {
        var permissions = permissionManager.GetPermissions();
        return Ok(permissions);
    }
}
