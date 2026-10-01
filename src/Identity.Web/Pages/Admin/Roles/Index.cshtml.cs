using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Modules.Identity.Application.Common.Models;
using StarterKit.Modules.Identity.Application.Roles.Commands;
using StarterKit.Modules.Identity.Application.Roles.Services;
using StarterKit.Modules.Identity.Contracts.Authorization;
using StarterKit.Modules.Identity.Web.Admin;
using System.ComponentModel.DataAnnotations;

namespace StarterKit.Modules.Identity.Web.Pages.Admin.Roles;

[Authorize(Policy = IdentityPermissions.Roles.View)]
public class IndexModel(IRoleService roleService) : AdminPageModel
{
    public IReadOnlyList<RoleRow> Rows { get; private set; } = [];

    public CreateRoleInput Create { get; set; } = new();

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostCreateAsync([Bind(Prefix = nameof(Create))] CreateRoleInput create)
    {
        if (!await IsAuthorizedAsync(IdentityPermissions.Roles.Manage))
            return Forbid();

        Create = create;

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var request = new CreateRoleRequest
        {
            Name = create.Name.Trim(),
            Description = create.Description,
        };

        var result = await SendAsync(
            new CreateRoleCommand(request),
            inputPrefix: nameof(Create));

        if (result is null)
        {
            await LoadAsync();
            return Page();
        }

        var handled = HandleResult(
            result,
            $"Role {request.Name} was created. Choose its permissions below.",
            () => RedirectToPage("/Admin/Roles/Edit", new { id = result.Data }));

        if (handled is not null)
            return handled;

        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        if (!await IsAuthorizedAsync(IdentityPermissions.Roles.Manage))
            return Forbid();

        var result = await SendAsync(new DeleteRoleCommand(id));

        return RedirectWithResult(result, "The role was deleted.");
    }

    private async Task LoadAsync()
    {
        var roles = await roleService.GetAllWithClaimsAsync();

        Rows = [.. roles.Select(r => new RoleRow(r, RolePermissionClaims.Permissions(r.Claims).Count))];
    }

    public sealed record RoleRow(
        RoleDto Role,
        int PermissionCount);

    public sealed class CreateRoleInput
    {
        [Required]
        [StringLength(256)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }
    }
}
