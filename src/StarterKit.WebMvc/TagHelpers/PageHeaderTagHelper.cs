using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace StarterKit.WebMvc.TagHelpers;

/// <summary>
/// Page title block with optional breadcrumb and actions:
/// <code>
/// &lt;page-header title="Users" subtitle="Manage sign-in accounts."&gt;
///     &lt;breadcrumb-item href="/"&gt;Home&lt;/breadcrumb-item&gt;
///     &lt;breadcrumb-item&gt;Users&lt;/breadcrumb-item&gt;
///     &lt;page-actions&gt;&lt;a class="btn btn-primary" ...&gt;New user&lt;/a&gt;&lt;/page-actions&gt;
/// &lt;/page-header&gt;
/// </code>
/// Also sets <c>ViewData["Title"]</c> when the view has not.
/// </summary>
[HtmlTargetElement("page-header")]
[RestrictChildren("breadcrumb-item", "page-actions")]
public sealed class PageHeaderTagHelper : TagHelper
{
    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    [Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContext]
    [HtmlAttributeNotBound]
    public Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { get; set; } = null!;

    public override async Task ProcessAsync(
        TagHelperContext context,
        TagHelperOutput output)
    {
        var parts = new PageHeaderParts();
        context.Items[typeof(PageHeaderParts)] = parts;

        await output.GetChildContentAsync();

        if (ViewContext.ViewData["Title"] is null)
        {
            ViewContext.ViewData["Title"] = Title;
        }

        output.TagName = "header";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "page-header mb-4");

        if (parts.Breadcrumbs.Count > 0)
        {
            output.Content.AppendHtml(RenderBreadcrumb(parts.Breadcrumbs));
        }

        var row = new TagBuilder("div");
        row.AddCssClass("d-flex flex-column flex-sm-row align-items-sm-center justify-content-between gap-2");

        var titles = new TagBuilder("div");
        titles.AddCssClass("min-w-0");

        var heading = new TagBuilder("h1");
        heading.AddCssClass("h3 mb-1 text-break");
        heading.InnerHtml.Append(Title);
        titles.InnerHtml.AppendHtml(heading);

        if (!string.IsNullOrEmpty(Subtitle))
        {
            var subtitle = new TagBuilder("p");
            subtitle.AddCssClass("text-body-secondary mb-0");
            subtitle.InnerHtml.Append(Subtitle);
            titles.InnerHtml.AppendHtml(subtitle);
        }

        row.InnerHtml.AppendHtml(titles);

        if (parts.Actions is not null)
        {
            var actions = new TagBuilder("div");
            actions.AddCssClass("d-flex flex-wrap gap-2 flex-shrink-0");
            actions.InnerHtml.AppendHtml(parts.Actions);
            row.InnerHtml.AppendHtml(actions);
        }

        output.Content.AppendHtml(row);
    }

    private static IHtmlContent RenderBreadcrumb(IReadOnlyList<(string? Href, IHtmlContent Content)> items)
    {
        var nav = new TagBuilder("nav");
        nav.Attributes["aria-label"] = "breadcrumb";

        var list = new TagBuilder("ol");
        list.AddCssClass("breadcrumb small mb-2");

        for (var i = 0; i < items.Count; i++)
        {
            var (href, content) = items[i];
            var isLast = i == items.Count - 1;

            var item = new TagBuilder("li");
            item.AddCssClass(isLast ? "breadcrumb-item active" : "breadcrumb-item");

            if (isLast)
            {
                item.Attributes["aria-current"] = "page";
            }

            if (!isLast && !string.IsNullOrEmpty(href))
            {
                var link = new TagBuilder("a");
                link.Attributes["href"] = href;
                link.InnerHtml.AppendHtml(content);
                item.InnerHtml.AppendHtml(link);
            }
            else
            {
                item.InnerHtml.AppendHtml(content);
            }

            list.InnerHtml.AppendHtml(item);
        }

        nav.InnerHtml.AppendHtml(list);
        return nav;
    }
}

internal sealed class PageHeaderParts
{
    public List<(string? Href, IHtmlContent Content)> Breadcrumbs { get; } = [];

    public IHtmlContent? Actions { get; set; }
}

[HtmlTargetElement("breadcrumb-item", ParentTag = "page-header")]
public sealed class BreadcrumbItemTagHelper : TagHelper
{
    public string? Href { get; set; }

    public override async Task ProcessAsync(
        TagHelperContext context,
        TagHelperOutput output)
    {
        if (context.Items.TryGetValue(typeof(PageHeaderParts), out var value) && value is PageHeaderParts parts)
        {
            parts.Breadcrumbs.Add((Href, await output.GetChildContentAsync()));
        }

        output.SuppressOutput();
    }
}

[HtmlTargetElement("page-actions", ParentTag = "page-header")]
public sealed class PageActionsTagHelper : TagHelper
{
    public override async Task ProcessAsync(
        TagHelperContext context,
        TagHelperOutput output)
    {
        if (context.Items.TryGetValue(typeof(PageHeaderParts), out var value) && value is PageHeaderParts parts)
        {
            var content = await output.GetChildContentAsync();
            parts.Actions = content.IsEmptyOrWhiteSpace ? null : content;
        }

        output.SuppressOutput();
    }
}
