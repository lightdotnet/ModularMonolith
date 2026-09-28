using Microsoft.AspNetCore.Mvc;
using StarterKit.Identity.Contracts;
using StarterKit.Identity.Contracts.Authorization;
using StarterKit.WebMvc.Authorization;
using StarterKit.WebMvc.Services.Identity;
using StarterKit.WebMvc.Web;
using StarterKit.WebMvc.Web.Flash;

namespace StarterKit.WebMvc.Pages.Identity.Users;

/// <summary>
/// Edits a user's profile, status, auth provider and roles. Only those fields are bound from the
/// form (<see cref="UserEditInput"/>); on save the current <see cref="UserDto"/> is reloaded from
/// the backend and the edits are merged into it, because <c>PUT user/{id}</c> replaces roles and
/// claims wholesale — claims, id and username therefore never round-trip through the browser.
/// </summary>
[HasPermission(IdentityPermissions.Users.Update)]
public sealed class EditModel(
    IUserClient userClient,
    IRoleClient roleClient)
    : WebPageModel
{
    [BindProperty]
    public UserEditInput Input { get; set; } = new();

    /// <summary>The user as last loaded from the backend (read-only display: username, claims).</summary>
    public UserDto EditedUser { get; private set; } = new();

    public IReadOnlyList<string> AvailableRoles { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(
        string id,
        CancellationToken cancellationToken)
    {
        if (!await LoadUserAsync(
            id,
            cancellationToken))
        {
            return RedirectToPage("./Index");
        }

        Input = UserEditInput.From(EditedUser);
        await LoadRolesAsync(cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        string id,
        CancellationToken cancellationToken)
    {
        if (!await LoadUserAsync(
            id,
            cancellationToken))
        {
            return RedirectToPage("./Index");
        }

        if (!ModelState.IsValid)
        {
            await LoadRolesAsync(cancellationToken);
            return Page();
        }

        var result = await userClient.UpdateAsync(
            Input.ApplyTo(EditedUser),
            cancellationToken);

        if (!HandleApiResult(
            result,
            $"User \"{EditedUser.UserName}\" updated."))
        {
            await LoadRolesAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage("./Index");
    }

    private async Task<bool> LoadUserAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await userClient.GetByIdAsync(
            id,
            cancellationToken);

        if (!result.IsSuccess || result.Data is null)
        {
            Flash(
                FlashType.Error,
                string.IsNullOrWhiteSpace(result.Message) || result.IsSuccess ? "User not found." : result.Message);

            return false;
        }

        EditedUser = result.Data;
        return true;
    }

    private async Task LoadRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await roleClient.GetAllAsync(cancellationToken);

        // Roles the user holds (or just ticked) stay listed even if the role list could not be loaded.
        AvailableRoles = (roles.Data?.Select(role => role.Name) ?? [])
            .Union(
                Input.Roles,
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(
                name => name,
                StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!roles.IsSuccess)
        {
            ModelState.AddModelError(
                string.Empty,
                $"Roles could not be loaded: {roles.Message}");
        }
    }

    /// <summary>The editable subset of <see cref="UserDto"/>.</summary>
    public sealed class UserEditInput
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Status { get; set; }

        public string? AuthProvider { get; set; }

        public List<string> Roles { get; set; } = [];

        public static UserEditInput From(UserDto user)
        {
            return new UserEditInput
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Status = user.Status,
                AuthProvider = user.AuthProvider,
                Roles = user.Roles.ToList(),
            };
        }

        /// <summary>Copies the edited fields onto the freshly loaded <paramref name="user"/> and returns it.</summary>
        public UserDto ApplyTo(UserDto user)
        {
            user.FirstName = FirstName;
            user.LastName = LastName;
            user.Email = Email;
            user.PhoneNumber = PhoneNumber;
            user.Status = Status;
            user.AuthProvider = AuthProvider;
            user.Roles = Roles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return user;
        }
    }
}
