using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Modules.Identity.Application.Common.Models;
using StarterKit.Modules.Identity.Application.Roles.Commands;
using StarterKit.Modules.Identity.Application.Roles.Services;
using StarterKit.Modules.Identity.Contracts.Authorization;
using StarterKit.Modules.Identity.Web.Admin;

namespace StarterKit.Modules.Identity.Web.Pages.Admin.Permissions;

/// <summary>
/// Role x permission matrix: read-only with <c>roles.view</c>, editable (saved per role) with
/// <c>roles.manage</c>.
/// </summary>
[Authorize(Policy = IdentityPermissions.Roles.View)]
public class IndexModel(
    IRoleService roleService,
    IPermissionManager permissionManager)
    : AdminPageModel
{
    private Dictionary<string, HashSet<string>> _granted = [];

    public IReadOnlyList<RoleDto> Roles { get; private set; } = [];

    public IReadOnlyList<PermissionGroup> Groups { get; private set; } = [];

    public bool CanManage { get; private set; }

    public async Task OnGetAsync()
    {
        CanManage = await IsAuthorizedAsync(IdentityPermissions.Roles.Manage);
        Roles = await roleService.GetAllWithClaimsAsync();
        Groups = PermissionGroup.From(permissionManager.GetPermissions());

        _granted = Roles.ToDictionary(
            r => r.Id,
            r => RolePermissionClaims.Permissions(r.Claims));
    }

    public async Task<IActionResult> OnPostSaveAsync(
        string roleId,
        List<string>? permissions)
    {
        if (!await IsAuthorizedAsync(IdentityPermissions.Roles.Manage))
            return Forbid();

        var role = await roleService.GetByIdAsync(roleId);
        if (!role.IsSuccess || role.Data is null)
            return RedirectWithResult(role, string.Empty);

        // Only the permissions the signed-in user may grant are replaced by the selection; the
        // others are rendered disabled (not posted) and kept as stored by the merge.
        var changeable = permissionManager
            .GetPermissions()
            .Select(p => p.Name)
            .Where(CanGrant);

        // Name and description are sent back unchanged; the role's non-permission claims and
        // permissions unknown to this host are kept by the merge.
        var request = new RoleDto
        {
            Id = role.Data.Id,
            Name = role.Data.Name,
            Description = role.Data.Description,
            Claims = RolePermissionClaims.Merge(role.Data.Claims, permissions ?? [], changeable),
        };

        var result = await SendAsync(
            new UpdateRoleCommand(request),
            inputPrefix: string.Empty);

        return RedirectWithResult(result, $"Permissions of role {request.Name} were saved.");
    }

    public bool Grants(RoleDto role, string permission) =>
        _granted.TryGetValue(role.Id, out var granted) && granted.Contains(permission);
}
