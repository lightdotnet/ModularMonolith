using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Modules.Identity.Application.Common.Models;
using StarterKit.Modules.Identity.Application.Roles.Commands;
using StarterKit.Modules.Identity.Application.Roles.Services;
using StarterKit.Modules.Identity.Contracts.Authorization;
using StarterKit.Modules.Identity.Web.Admin;
using System.ComponentModel.DataAnnotations;

namespace StarterKit.Modules.Identity.Web.Pages.Admin.Roles;

[Authorize(Policy = IdentityPermissions.Roles.Manage)]
public class EditModel(
    IRoleService roleService,
    IPermissionManager permissionManager)
    : AdminPageModel
{
    public RoleDto Role { get; private set; } = null!;

    public EditRoleInput Input { get; set; } = new();

    public IReadOnlyList<PermissionGroup> Groups { get; private set; } = [];

    /// <summary>
    /// Permission claims on the role that no loaded module defines; kept unchanged on save.
    /// </summary>
    public IReadOnlyList<string> OtherPermissions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!await LoadAsync(id))
            return NotFound();

        Input = new EditRoleInput
        {
            Name = Role.Name,
            Description = Role.Description,
            Permissions = [.. RolePermissionClaims.Permissions(Role.Claims)],
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        string id,
        [Bind(Prefix = nameof(Input))] EditRoleInput input)
    {
        Input = input;

        if (!await LoadAsync(id))
            return NotFound();

        if (!ModelState.IsValid)
            return Page();

        // Only the permissions the signed-in user may grant are replaced by the selection; the
        // others are rendered disabled (not posted) and kept as stored by the merge.
        var changeable = Groups
            .SelectMany(g => g.Permissions)
            .Select(p => p.Name)
            .Where(CanGrant);

        var request = new RoleDto
        {
            Id = Role.Id,
            Name = input.Name.Trim(),
            Description = input.Description,
            Claims = RolePermissionClaims.Merge(Role.Claims, input.Permissions, changeable),
        };

        // Re-rendered on failure: show the merged selection, disabled permissions included.
        input.Permissions = [.. RolePermissionClaims.Permissions(request.Claims)];

        var result = await SendAsync(new UpdateRoleCommand(request));

        if (result is null)
            return Page();

        return HandleResult(
            result,
            $"Role {request.Name} was updated.",
            () => RedirectToPage(new { id }))
            ?? Page();
    }

    private async Task<bool> LoadAsync(string id)
    {
        var role = await roleService.GetByIdAsync(id);
        if (!role.IsSuccess || role.Data is null)
            return false;

        Role = role.Data;
        Groups = PermissionGroup.From(permissionManager.GetPermissions());

        var known = Groups
            .SelectMany(g => g.Permissions)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        OtherPermissions =
        [
            .. RolePermissionClaims
                .Permissions(Role.Claims)
                .Where(p => !known.Contains(p))
                .Order()
        ];
        return true;
    }

    public sealed class EditRoleInput
    {
        [Required]
        [StringLength(256)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        public List<string> Permissions { get; set; } = [];
    }
}
