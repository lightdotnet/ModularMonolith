using Light.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Modules.Identity.Application.Common.Models;
using StarterKit.Modules.Identity.Application.Users.Commands;
using StarterKit.Modules.Identity.Application.Users.Queries;
using StarterKit.Modules.Identity.Contracts.Authorization;
using StarterKit.Shared.Authorization;

namespace StarterKit.Modules.Identity.Web.Pages.Admin.Users;

[Authorize(Policy = IdentityPermissions.Users.View)]
public class IndexModel : AdminPageModel
{
    private const int MaxPageSize = 100;

    public Paged<UserDto> Result { get; private set; } = new();

    public async Task OnGetAsync([FromQuery] SearchUserRequest query)
    {
        query.PageNumber = Math.Max(query.PageNumber, 1);
        query.PageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var result = await SendAsync(
            new SearchUserQuery(query),
            inputPrefix: string.Empty);

        Result = result?.Data ?? new Paged<UserDto>([], query.PageNumber, query.PageSize, 0);
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        if (!await IsAuthorizedAsync(IdentityPermissions.Users.Delete))
            return Forbid();

        var result = await SendAsync(new DeleteUserCommand(id));

        return RedirectWithResult(result, "The user was deleted.");
    }

    /// <summary>
    /// Whether the delete action is offered for <paramref name="user"/>: never for the signed-in
    /// user's own account or a super user (the server-side guard rejects both anyway).
    /// </summary>
    public bool CanDelete(UserDto user) =>
        !IsSelf(user.Id)
        && !SuperUserPolicy.IsSuper(user.UserName);

    public static string? FullName(UserDto user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return name.Length == 0 ? null : name;
    }
}
