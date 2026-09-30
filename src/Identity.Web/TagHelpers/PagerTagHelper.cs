using Light.Contracts;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Primitives;

namespace StarterKit.Modules.Identity.Web.TagHelpers;

/// <summary>
/// <c>&lt;pager model="Model.Result" /&gt;</c> renders a record summary and Bootstrap pagination
/// for any <see cref="IPaged"/>. Page links keep the current query string and only replace the
/// page number. On small screens only the previous/next links and the current page are shown.
/// Nothing is rendered for an empty result.
/// </summary>
[HtmlTargetElement("pager", TagStructure = TagStructure.WithoutEndTag)]
public sealed class PagerTagHelper : TagHelper
{
    public IPaged? Model { get; set; }

    /// <summary>
    /// Number of page links shown on each side of the current page.
    /// </summary>
    public int Window { get; set; } = 2;

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (Model is null || Model.TotalRecords <= 0)
        {
            output.SuppressOutput();
            return;
        }

        var request = ViewContext.HttpContext.Request;
        var totalPages = Math.Max(Model.TotalPages, 1);
        var current = Math.Clamp(Model.PageNumber, 1, totalPages);

        output.TagName = "nav";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("aria-label", "Pagination");
        output.Attributes.SetAttribute(
            "class",
            "d-flex flex-column flex-md-row align-items-center justify-content-between gap-2 mt-3");

        var content = new HtmlContentBuilder();
        content.AppendHtml(Summary(Model, current));

        if (totalPages > 1)
        {
            var list = new TagBuilder("ul");
            list.AddCssClass("pagination flex-wrap justify-content-center mb-0");

            list.InnerHtml.AppendHtml(Item(
                request,
                page: current - 1,
                ariaLabel: "Previous",
                text: "‹",
                disabled: current <= 1,
                active: false,
                visibleOnMobile: true));

            foreach (var page in PageNumbers(current, totalPages, Window))
            {
                if (page is null)
                {
                    list.InnerHtml.AppendHtml(Ellipsis());
                    continue;
                }

                list.InnerHtml.AppendHtml(Item(
                    request,
                    page: page.Value,
                    ariaLabel: null,
                    text: page.Value.ToString(),
                    disabled: false,
                    active: page == current,
                    visibleOnMobile: page == current));
            }

            list.InnerHtml.AppendHtml(Item(
                request,
                page: current + 1,
                ariaLabel: "Next",
                text: "›",
                disabled: current >= totalPages,
                active: false,
                visibleOnMobile: true));

            content.AppendHtml(list);
        }

        output.Content.SetHtmlContent(content);
    }

    /// <summary>
    /// Page numbers to link, with <c>null</c> marking a gap: always the first and last page,
    /// plus <paramref name="window"/> pages around the current one.
    /// </summary>
    public static IEnumerable<int?> PageNumbers(int current, int totalPages, int window)
    {
        var from = Math.Max(1, current - window);
        var to = Math.Min(totalPages, current + window);

        if (from > 1)
        {
            yield return 1;
            if (from > 2)
                yield return null;
        }

        for (var page = from; page <= to; page++)
            yield return page;

        if (to < totalPages)
        {
            if (to < totalPages - 1)
                yield return null;
            yield return totalPages;
        }
    }

    /// <summary>
    /// The current request URL with the page-number query value replaced by <paramref name="page"/>.
    /// </summary>
    public static string PageUrl(HttpRequest request, int page)
    {
        var pageKey = SearchFormTagHelper.PageNumberName;

        var values = request.Query
            .Where(x => !string.Equals(x.Key, pageKey, StringComparison.OrdinalIgnoreCase))
            .Append(new KeyValuePair<string, StringValues>(pageKey, page.ToString()));

        return $"{request.PathBase}{request.Path}{QueryString.Create(values)}";
    }

    private static TagBuilder Summary(IPaged model, int current)
    {
        var first = ((current - 1) * model.PageSize) + 1;
        var last = Math.Min(current * model.PageSize, model.TotalRecords);

        var summary = new TagBuilder("div");
        summary.AddCssClass("text-body-secondary small");
        summary.InnerHtml.Append($"Showing {first}–{last} of {model.TotalRecords}");
        return summary;
    }

    private static TagBuilder Item(
        HttpRequest request,
        int page,
        string? ariaLabel,
        string text,
        bool disabled,
        bool active,
        bool visibleOnMobile)
    {
        var item = new TagBuilder("li");
        item.AddCssClass("page-item");

        if (disabled)
            item.AddCssClass("disabled");

        if (active)
            item.AddCssClass("active");

        if (!visibleOnMobile)
            item.AddCssClass("d-none d-sm-block");

        TagBuilder link;
        if (disabled || active)
        {
            link = new TagBuilder("span");
            if (active)
                link.Attributes["aria-current"] = "page";
        }
        else
        {
            link = new TagBuilder("a");
            link.Attributes["href"] = PageUrl(request, page);
        }

        link.AddCssClass("page-link");

        if (ariaLabel is not null)
            link.Attributes["aria-label"] = ariaLabel;

        link.InnerHtml.Append(text);
        item.InnerHtml.AppendHtml(link);
        return item;
    }

    private static TagBuilder Ellipsis()
    {
        var item = new TagBuilder("li");
        item.AddCssClass("page-item disabled d-none d-sm-block");

        var span = new TagBuilder("span");
        span.AddCssClass("page-link");
        span.InnerHtml.Append("…");

        item.InnerHtml.AppendHtml(span);
        return item;
    }
}
