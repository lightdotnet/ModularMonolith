using Microsoft.AspNetCore.Mvc;
using StarterKit.Identity.Contracts;
using StarterKit.Identity.Contracts.Authorization;
using StarterKit.WebMvc.Authorization;
using StarterKit.WebMvc.Services.Identity;
using StarterKit.WebMvc.Web;
using StarterKit.WebMvc.Web.DataTables;

namespace StarterKit.WebMvc.Pages.Identity.Users;

/// <summary>
/// User list over <c>GET user/search</c> (search + server paging + server sort; sortable column keys
/// are mapped to backend fields by <see cref="DataTableSortMaps.Users"/>).
/// </summary>
[HasPermission(IdentityPermissions.Users.View)]
public sealed class IndexModel(
    IUserClient userClient)
    : WebPageModel
{
    public DataTableResult<UserDto> Table { get; private set; } = DataTableResult<UserDto>.Empty(PagedQuery.Default);

    public async Task<IActionResult> OnGetAsync(
        PagedQuery query,
        CancellationToken cancellationToken)
    {
        var sort = DataTableSortMaps.ForUsers(query);

        var result = await userClient.SearchAsync(
            new SearchUserRequest
            {
                SearchValue = query.Search,
                PageNumber = query.Page,
                PageSize = query.PageSize,
                SortBy = sort?.SortBy,
                SortDirection = sort?.SortDirection,
            },
            cancellationToken);

        Table = DataTableResult<UserDto>.FromApi(
            result,
            query);

        return PageOrDataTable("_UsersTable");
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        string id,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        if (!Can(IdentityPermissions.Users.Delete))
        {
            return Forbid();
        }

        var result = await userClient.DeleteAsync(
            id,
            cancellationToken);

        FlashApiResult(
            result,
            "User deleted.");

        return RedirectToLocal(
            returnUrl,
            "./Index");
    }
}
