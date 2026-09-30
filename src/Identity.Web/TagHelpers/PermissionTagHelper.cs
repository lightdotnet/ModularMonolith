using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace StarterKit.Modules.Identity.Web.TagHelpers;

/// <summary>
/// Renders the element only when the current user satisfies at least one of the
/// comma-separated authorization policies in <c>asp-permission</c> (permission names resolve
/// to policies through the shared permission policy provider).
/// </summary>
/// <remarks>
/// Runs after the other tag helpers on the same element so its suppression is final.
/// This only hides UI; the page/handler must still authorize the action itself.
/// </remarks>
[HtmlTargetElement(Attributes = AttributeName)]
public sealed class PermissionTagHelper(IAuthorizationService authorizationService) : TagHelper
{
    public const string AttributeName = "asp-permission";

    [HtmlAttributeName(AttributeName)]
    public string? Permission { get; set; }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override int Order => 1000;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var policies = (Permission ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var user = ViewContext.HttpContext.User;

        foreach (var policy in policies)
        {
            var result = await authorizationService.AuthorizeAsync(user, policy);
            if (result.Succeeded)
                return;
        }

        output.SuppressOutput();
    }
}
