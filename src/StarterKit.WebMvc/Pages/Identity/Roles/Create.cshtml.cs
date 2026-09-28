using Microsoft.AspNetCore.Mvc;
using StarterKit.Identity.Contracts;
using StarterKit.Identity.Contracts.Authorization;
using StarterKit.WebMvc.Authorization;
using StarterKit.WebMvc.Services.Identity;
using StarterKit.WebMvc.Web;

namespace StarterKit.WebMvc.Pages.Identity.Roles;

/// <summary>Creates a role, then continues to its edit page to assign permissions.</summary>
[HasPermission(IdentityPermissions.Roles.Manage)]
public sealed class CreateModel(
    IRoleClient roleClient)
    : WebPageModel
{
    [BindProperty]
    public CreateRoleRequest Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await roleClient.CreateAsync(
            Input,
            cancellationToken);

        if (!HandleApiResult(
            result,
            $"Role \"{Input.Name}\" created. Assign its permissions below."))
        {
            return Page();
        }

        return string.IsNullOrEmpty(result.Data)
            ? RedirectToPage("./Index")
            : RedirectToPage(
                "./Edit",
                new { id = result.Data });
    }
}
