using Microsoft.AspNetCore.Mvc;
using StarterKit.Identity.Contracts;
using StarterKit.Identity.Contracts.Authorization;
using StarterKit.WebMvc.Authorization;
using StarterKit.WebMvc.Services.Identity;
using StarterKit.WebMvc.Web;

namespace StarterKit.WebMvc.Pages.Identity.Users;

[HasPermission(IdentityPermissions.Users.Create)]
public sealed class CreateModel(
    IUserClient userClient)
    : WebPageModel
{
    [BindProperty]
    public CreateUserRequest Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (IdentityOptions.RequiresPassword(Input.AuthProvider) && string.IsNullOrWhiteSpace(Input.Password))
        {
            ModelState.AddModelError(
                $"{nameof(Input)}.{nameof(Input.Password)}",
                "Password is required for local accounts.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!IdentityOptions.RequiresPassword(Input.AuthProvider))
        {
            Input.Password = null;
        }

        var result = await userClient.CreateAsync(
            Input,
            cancellationToken);

        if (!HandleApiResult(
            result,
            $"User \"{Input.UserName}\" created."))
        {
            return Page();
        }

        return RedirectToPage("./Index");
    }
}
