using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StarterKit.Identity.Api.Entities;

namespace StarterKit.Identity.Web.Pages;

public class IndexModel(UserManager<User> userManager) : PageModel
{
    public UserInfo? CurrentUser { get; private set; }

    public async Task OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
            return;

        var userId = userManager.GetUserId(User);
        var user = userId is null ? null : await userManager.FindByIdAsync(userId);
        if (user is null)
            return;

        CurrentUser = new UserInfo(
            user.UserName,
            user.Email,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.AuthProvider.ToString(),
            user.Id);
    }

    public sealed record UserInfo(
        string? UserName,
        string? Email,
        string? DisplayName,
        string? AuthProvider,
        string Id);
}
