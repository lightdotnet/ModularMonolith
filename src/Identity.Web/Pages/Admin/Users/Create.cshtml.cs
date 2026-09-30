using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StarterKit.Modules.Identity.Authorization;
using StarterKit.Modules.Identity.Features.Users.Commands;
using StarterKit.Modules.Identity.Models;
using System.ComponentModel.DataAnnotations;

namespace StarterKit.Modules.Identity.Web.Pages.Admin.Users;

[Authorize(Policy = IdentityPermissions.Users.Create)]
public class CreateModel : AdminPageModel
{
    [BindProperty]
    public CreateUserInput Input { get; set; } = new();

    public IEnumerable<SelectListItem> AuthProviders => UserFormOptions.AuthProviders();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var request = new CreateUserRequest
        {
            UserName = Input.UserName.Trim(),
            FirstName = Input.FirstName,
            LastName = Input.LastName,
            Email = Input.Email,
            PhoneNumber = Input.PhoneNumber,
            Password = Input.Password,
            AuthProvider = string.IsNullOrEmpty(Input.AuthProvider) ? null : Input.AuthProvider,
        };

        var result = await SendAsync(new CreateUserCommand(request));

        if (result is null)
            return Page();

        // Continue to the edit page (roles are assigned there) when the user may edit.
        var canEdit = await IsAuthorizedAsync(IdentityPermissions.Users.Update);

        return HandleResult(
            result,
            $"User {request.UserName} was created.",
            () => canEdit
                ? RedirectToPage("/Admin/Users/Edit", new { id = result.Data })
                : RedirectToPage("/Admin/Users/Index"))
            ?? Page();
    }

    public sealed class CreateUserInput
    {
        [Required]
        [StringLength(256)]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

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

        [Display(Name = "Sign-in provider")]
        public string? AuthProvider { get; set; }

        [StringLength(100)]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}
