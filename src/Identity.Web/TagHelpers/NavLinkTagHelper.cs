using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace StarterKit.Modules.Identity.Web.TagHelpers;

/// <summary>
/// <c>&lt;nav-link asp-page="/Admin/Users/Index" icon="people"&gt;Users&lt;/nav-link&gt;</c>
/// renders a Bootstrap <c>nav-link</c> anchor, marked active when the current page is the
/// target page or (with the default <c>match="folder"</c>) any page in the target's folder.
/// </summary>
[HtmlTargetElement("nav-link")]
public sealed class NavLinkTagHelper(IUrlHelperFactory urlHelperFactory) : TagHelper
{
    [HtmlAttributeName("asp-page")]
    public string Page { get; set; } = null!;

    /// <summary>
    /// Optional Bootstrap Icons name (without the <c>bi-</c> prefix).
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// <c>folder</c> (default) or <c>exact</c>.
    /// </summary>
    public string Match { get; set; } = "folder";

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var urlHelper = urlHelperFactory.GetUrlHelper(ViewContext);
        var currentPage = ViewContext.RouteData.Values["page"] as string;
        var active = IsActive(currentPage, Page, Match);

        output.TagName = "a";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("href", urlHelper.Page(Page));
        output.AddClass("nav-link", HtmlEncoder.Default);

        if (active)
        {
            output.AddClass("active", HtmlEncoder.Default);
            output.Attributes.SetAttribute("aria-current", "page");
        }

        var childContent = await output.GetChildContentAsync();

        if (!string.IsNullOrWhiteSpace(Icon))
        {
            var icon = new TagBuilder("i");
            icon.AddCssClass($"bi bi-{Icon} me-2");
            icon.Attributes["aria-hidden"] = "true";
            output.Content.AppendHtml(icon);
        }

        output.Content.AppendHtml(childContent);
    }

    public static bool IsActive(string? currentPage, string targetPage, string match)
    {
        if (string.IsNullOrEmpty(currentPage))
            return false;

        if (string.Equals(currentPage, targetPage, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.Equals(match, "folder", StringComparison.OrdinalIgnoreCase))
            return false;

        var slash = targetPage.LastIndexOf('/');
        if (slash <= 0)
            return false;

        var folder = targetPage[..(slash + 1)];
        return currentPage.StartsWith(folder, StringComparison.OrdinalIgnoreCase);
    }
}
