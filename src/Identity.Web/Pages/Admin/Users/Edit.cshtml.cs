using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StarterKit.Modules.Identity.Authorization;
using StarterKit.Modules.Identity.Features.Users.Commands;
using StarterKit.Modules.Identity.Models;
using StarterKit.Modules.Identity.Services;
using StarterKit.Modules.Identity.Web.Admin;
using System.ComponentModel.DataAnnotations;

namespace StarterKit.Modules.Identity.Web.Pages.Admin.Users;

[Authorize(Policy = IdentityPermissions.Users.Update)]
public class EditModel(
    IUserService userService,
    IRoleService roleService)
    : AdminPageModel
{
    public UserDto Account { get; private set; } = null!;

    public EditUserInput Input { get; set; } = new();

    public ForcePasswordInput Password { get; set; } = new();

    public IReadOnlyList<RoleDto> Roles { get; private set; } = [];

    public IEnumerable<SelectListItem> AuthProviders => UserFormOptions.AuthProviders();

    public IEnumerable<SelectListItem> Statuses => UserFormOptions.Statuses(Account.Status);

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!await LoadAsync(id))
            return NotFound();

        Input = EditUserInput.From(Account);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        string id,
        [Bind(Prefix = nameof(Input))] EditUserInput input)
    {
        Input = input;

        if (!await LoadAsync(id))
            return NotFound();

        if (IsEditingSelf)
        {
            // Own roles and status are rendered disabled (not posted): keep the stored values.
            ModelState.Remove($"{nameof(Input)}.{nameof(EditUserInput.Status)}");
            input.Status = Account.Status;
        }

        input.Roles = MergeRoles(input.Roles);

        if (!ModelState.IsValid)
            return Page();

        // Start from the stored user so the fields this form does not edit (user name,
        // direct claims) are sent back unchanged: UpdateUserCommand syncs roles and claims.
        Account.FirstName = input.FirstName;
        Account.LastName = input.LastName;
        Account.Email = input.Email;
        Account.PhoneNumber = input.PhoneNumber;
        Account.Status = input.Status;
        Account.AuthProvider = string.IsNullOrEmpty(input.AuthProvider) ? null : input.AuthProvider;
        Account.Roles = [.. input.Roles];

        var result = await SendAsync(new UpdateUserCommand(Account));

        if (result is null)
            return Page();

        return HandleResult(
            result,
            $"User {Account.UserName} was updated.",
            () => RedirectToPage(new { id }))
            ?? Page();
    }

    public async Task<IActionResult> OnPostPasswordAsync(
        string id,
        [Bind(Prefix = nameof(Password))] ForcePasswordInput password)
    {
        Password = password;

        if (!await LoadAsync(id))
            return NotFound();

        Input = EditUserInput.From(Account);

        if (!ModelState.IsValid)
            return Page();

        var passwordErrorKey = $"{nameof(Password)}.{nameof(ForcePasswordInput.NewPassword)}";

        var result = await SendAsync(
            new ForcePasswordCommand(id, password.NewPassword),
            inputPrefix: nameof(Password),
            unmappedKey: passwordErrorKey);

        if (result is null)
            return Page();

        return HandleResult(
            result,
            $"The password of {Account.UserName} was changed.",
            () => RedirectToPage(new { id }),
            errorKey: passwordErrorKey)
            ?? Page();
    }

    /// <summary>
    /// Whether the edited user is the signed-in user: own roles and status cannot be changed.
    /// </summary>
    public bool IsEditingSelf => IsSelf(Account.Id);

    /// <summary>
    /// Whether the signed-in user may assign or remove <paramref name="role"/>: not on their own
    /// account, and only when they hold every permission the role grants (or have full control).
    /// </summary>
    public bool CanAssign(RoleDto role) =>
        !IsEditingSelf
        && RolePermissionClaims.Permissions(role.Claims).All(CanGrant);

    /// <summary>
    /// The roles to save: the posted choice for the roles the signed-in user may assign, and the
    /// stored membership for the others (their checkboxes are disabled, so they are not posted).
    /// </summary>
    private List<string> MergeRoles(IEnumerable<string> posted)
    {
        var postedSet = posted.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var storedSet = Account.Roles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return
        [
            .. Roles
                .Where(r => CanAssign(r)
                    ? postedSet.Contains(r.Name)
                    : storedSet.Contains(r.Name))
                .Select(r => r.Name)
        ];
    }

    private async Task<bool> LoadAsync(string id)
    {
        var account = await userService.GetByIdAsync(id);
        if (!account.IsSuccess || account.Data is null)
            return false;

        Account = account.Data;
        Roles = await roleService.GetAllWithClaimsAsync();
        return true;
    }

    public sealed class EditUserInput
    {
        [StringLength(256)]
        [Display(Name = "First name")]
        public string? FirstName { get; set; }

        [StringLength(256)]
        [Display(Name = "Last name")]
        public string? LastName { get; set; }

        [EmailAddress]
        [StringLength(256)]
        [DataType(DataType.EmailAddress)]
        public string? Email { get; set; }

        [Phone]
        [StringLength(50)]
        [Display(Name = "Phone number")]
        [DataType(DataType.PhoneNumber)]
        public string? PhoneNumber { get; set; }

        [Required]
        public string? Status { get; set; }

        [Display(Name = "Sign-in provider")]
        public string? AuthProvider { get; set; }

        public List<string> Roles { get; set; } = [];

        public static EditUserInput From(UserDto user) => new()
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Status = user.Status,
            AuthProvider = user.AuthProvider ?? string.Empty,
            Roles = [.. user.Roles],
        };
    }

    public sealed class ForcePasswordInput
    {
        [Required]
        [StringLength(100)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
