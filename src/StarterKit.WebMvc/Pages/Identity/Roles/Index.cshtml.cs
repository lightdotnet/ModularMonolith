using Microsoft.AspNetCore.Mvc;
using StarterKit.Identity.Contracts;
using StarterKit.Identity.Contracts.Authorization;
using StarterKit.WebMvc.Authorization;
using StarterKit.WebMvc.Services.Identity;
using StarterKit.WebMvc.Web;
using StarterKit.WebMvc.Web.DataTables;

namespace StarterKit.WebMvc.Pages.Identity.Roles;

/// <summary>
/// Role list. <c>GET role</c> is not paged, so search, sort and paging happen in memory here over
/// the full list (fine for a role catalog; would need a backend search endpoint if it grew large).
/// </summary>
[HasPermission(IdentityPermissions.Roles.View)]
public sealed class IndexModel(
    IRoleClient roleClient)
    : WebPageModel
{
    private static readonly IReadOnlyDictionary<string, Func<RoleDto, object?>> SortKeys =
        new Dictionary<string, Func<RoleDto, object?>>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = role => role.Name,
            ["description"] = role => role.Description,
        };

    public DataTableResult<RoleDto> Table { get; private set; } = DataTableResult<RoleDto>.Empty(PagedQuery.Default);

    public async Task<IActionResult> OnGetAsync(
        PagedQuery query,
        CancellationToken cancellationToken)
    {
        var result = await roleClient.GetAllAsync(cancellationToken);

        Table = result.IsSuccess && result.Data is not null
            ? DataTableResult<RoleDto>.FromList(
                result.Data,
                query,
                (role, search) => role.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (role.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false),
                SortKeys)
            : DataTableResult<RoleDto>.Failure(
                result.Message,
                query);

        return PageOrDataTable("_RolesTable");
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        string id,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        if (!Can(IdentityPermissions.Roles.Manage))
        {
            return Forbid();
        }

        var result = await roleClient.DeleteAsync(
            id,
            cancellationToken);

        FlashApiResult(
            result,
            "Role deleted.");

        return RedirectToLocal(
            returnUrl,
            "./Index");
    }
}
