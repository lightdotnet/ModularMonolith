using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Identity.Contracts.Authorization;
using StarterKit.WebMvc.Authorization;
using StarterKit.WebMvc.Services.Identity;
using StarterKit.WebMvc.Web;
using StarterKit.WebMvc.Web.Flash;

namespace StarterKit.WebMvc.Pages.Identity.Users;

/// <summary>Sets a new password for a user (<c>PUT user/{id}/password/force</c>).</summary>
[HasPermission(IdentityPermissions.Users.Update)]
public sealed class PasswordModel(
    IUserClient userClient)
    : WebPageModel
{
    [BindProperty]
    public PasswordInput Input { get; set; } = new();

    public string? UserName { get; private set; }

    public string? AuthProvider { get; private set; }

    public async Task<IActionResult> OnGetAsync(
        string id,
        CancellationToken cancellationToken)
    {
        return await LoadUserAsync(
            id,
            cancellationToken)
            ? Page()
            : RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostAsync(
        string id,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await LoadUserAsync(
                id,
                cancellationToken)
                ? Page()
                : RedirectToPage("./Index");
        }

        var result = await userClient.ForcePasswordAsync(
            id,
            Input.NewPassword,
            cancellationToken);

        if (!HandleApiResult(
            result,
            "Password changed."))
        {
            return await LoadUserAsync(
                id,
                cancellationToken)
                ? Page()
                : RedirectToPage("./Index");
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

        UserName = result.Data.UserName;
        AuthProvider = result.Data.AuthProvider;
        return true;
    }

    public sealed class PasswordInput
    {
        [Required(ErrorMessage = "New password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm the password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
