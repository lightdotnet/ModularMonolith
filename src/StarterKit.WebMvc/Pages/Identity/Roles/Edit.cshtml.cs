using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Identity.Contracts;
using StarterKit.Identity.Contracts.Authorization;
using StarterKit.WebMvc.Authentication;
using StarterKit.WebMvc.Authorization;
using StarterKit.WebMvc.Services.Identity;
using StarterKit.WebMvc.Web;
using StarterKit.WebMvc.Web.Flash;

namespace StarterKit.WebMvc.Pages.Identity.Roles;

public sealed record PermissionGroup(
    string Name,
    IReadOnlyList<PermissionDefinition> Permissions);

/// <summary>
/// Edits a role's name/description and its permissions, grouped by the backend permission catalog
/// (<c>GET permissions</c>, grouped by <c>parent</c> like the admin client). <c>PUT role</c> replaces
/// the claim list, so claims that are not catalog permissions are carried through unchanged.
/// </summary>
[HasPermission(IdentityPermissions.Roles.Manage)]
public sealed class EditModel(
    IRoleClient roleClient,
    IPermissionClient permissionClient)
    : WebPageModel
{
    private const string OtherGroup = "other";

    [BindProperty]
    public RoleDto Input { get; set; } = new();

    /// <summary>Checked catalog permissions.</summary>
    [BindProperty]
    public List<string> SelectedPermissions { get; set; } = [];

    /// <summary>Claims that are not catalog permissions (kept as-is on save).</summary>
    [BindProperty]
    public List<ClaimDto> OtherClaims { get; set; } = [];

    public IReadOnlyList<PermissionGroup> PermissionGroups { get; private set; } = [];

    /// <summary>The permission catalog could not be loaded — the view then keeps the selection in hidden fields and disables saving.</summary>
    public bool PermissionCatalogUnavailable { get; private set; }

    public async Task<IActionResult> OnGetAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await roleClient.GetByIdAsync(
            id,
            cancellationToken);

        if (!result.IsSuccess || result.Data is null)
        {
            Flash(
                FlashType.Error,
                string.IsNullOrWhiteSpace(result.Message) ? "Role not found." : result.Message);

            return RedirectToPage("./Index");
        }

        Input = result.Data;

        var catalog = await LoadCatalogAsync(cancellationToken);

        // A permission claim only becomes a checkbox when the catalog knows it; everything else
        // (including every claim when the catalog failed to load) is preserved as an "other" claim.
        bool IsCatalogPermission(ClaimDto claim) =>
            claim.Type == SessionClaimTypes.Permission && catalog.Contains(claim.Value);

        SelectedPermissions = Input.Claims
            .Where(IsCatalogPermission)
            .Select(claim => claim.Value)
            .ToList();

        OtherClaims = Input.Claims
            .Where(claim => !IsCatalogPermission(claim))
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        string id,
        CancellationToken cancellationToken)
    {
        Input.Id = id;

        if (!ModelState.IsValid)
        {
            await LoadCatalogAsync(cancellationToken);
            return Page();
        }

        Input.Claims = OtherClaims
            .Concat(SelectedPermissions
                .Distinct(StringComparer.Ordinal)
                .Select(permission => new ClaimDto
                {
                    Type = SessionClaimTypes.Permission,
                    Value = permission,
                }))
            .ToList();

        var result = await roleClient.UpdateAsync(
            Input,
            cancellationToken);

        if (!HandleApiResult(
            result,
            $"Role \"{Input.Name}\" updated."))
        {
            await LoadCatalogAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage("./Index");
    }

    private async Task<HashSet<string>> LoadCatalogAsync(CancellationToken cancellationToken)
    {
        var result = await permissionClient.GetAllAsync(cancellationToken);

        if (!result.IsSuccess || result.Data is null)
        {
            ModelState.AddModelError(
                string.Empty,
                $"The permission list could not be loaded: {result.Message}");

            PermissionGroups = [];
            PermissionCatalogUnavailable = true;
            return [];
        }

        PermissionGroups = result.Data
            .GroupBy(permission => string.IsNullOrEmpty(permission.Parent) ? OtherGroup : permission.Parent)
            .Select(group => new PermissionGroup(
                group.Key,
                group.ToList()))
            .ToList();

        return result.Data
            .Select(permission => permission.Name)
            .ToHashSet(StringComparer.Ordinal);
    }
}
