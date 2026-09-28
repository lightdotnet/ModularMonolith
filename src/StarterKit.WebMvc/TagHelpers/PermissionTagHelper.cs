using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using StarterKit.WebMvc.Authorization;

namespace StarterKit.WebMvc.TagHelpers;

/// <summary>
/// Removes any element (and its content) the current user may not see:
/// <code>&lt;a asp-page="Create" asp-permission="@IdentityPermissions.Users.Create"&gt;New user&lt;/a&gt;</code>
/// Several permissions may be given comma-separated; <c>asp-permission-mode="all"</c> requires every
/// one of them (default <c>any</c>). This hides UI only — the page/action must still be gated.
/// </summary>
[HtmlTargetElement(Attributes = PermissionAttributeName)]
public sealed class PermissionTagHelper(
    IPermissionChecker permissionChecker)
    : TagHelper
{
    private const string PermissionAttributeName = "asp-permission";

    // Run after every other helper on the element: one that runs later (e.g. confirm-button setting
    // its TagName/content) would otherwise undo the suppression.
    public override int Order => int.MaxValue;

    [HtmlAttributeName(PermissionAttributeName)]
    public string? Permission { get; set; }

    [HtmlAttributeName("asp-permission-mode")]
    public string Mode { get; set; } = "any";

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(
        TagHelperContext context,
        TagHelperOutput output)
    {
        var permissions = (Permission ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (permissions.Length == 0)
        {
            return;
        }

        var user = ViewContext.HttpContext.User;

        var allowed = string.Equals(Mode, "all", StringComparison.OrdinalIgnoreCase)
            ? permissionChecker.HasAllPermissions(user, permissions)
            : permissionChecker.HasAnyPermission(user, permissions);

        if (!allowed)
        {
            output.SuppressOutput();
        }
    }
}
