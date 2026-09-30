using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using StarterKit.Shared;

namespace StarterKit.Modules.Identity.Web.TagHelpers;

/// <summary>
/// <c>&lt;search-form placeholder="Search users" /&gt;</c> renders a GET form with a search box
/// and a page-size select bound to the <see cref="SearchQuery"/> query-string names. Other
/// query-string values are carried over as hidden inputs; the page number is dropped so a new
/// search starts on page 1. Child content (extra filters) is placed before the submit button.
/// </summary>
[HtmlTargetElement("search-form")]
public sealed class SearchFormTagHelper : TagHelper
{
    public static readonly string SearchValueName = nameof(SearchQuery.SearchValue);

    public static readonly string PageSizeName = nameof(PageQuery.PageSize);

    public static readonly string PageNumberName = nameof(PageQuery.PageNumber);

    public string Placeholder { get; set; } = "Search";

    /// <summary>
    /// Comma-separated page sizes offered in the select.
    /// </summary>
    public string PageSizes { get; set; } = "10,20,50,100";

    /// <summary>
    /// Page size selected when the query string has none.
    /// </summary>
    public int DefaultPageSize { get; set; } = new PageQuery().PageSize;

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var query = ViewContext.HttpContext.Request.Query;
        var childContent = await output.GetChildContentAsync();

        output.TagName = "form";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("method", "get");
        output.Attributes.SetAttribute("role", "search");

        if (!output.Attributes.ContainsName("class"))
            output.Attributes.SetAttribute("class", "row g-2 align-items-center mb-3");

        var content = new HtmlContentBuilder();

        foreach (var (key, values) in query)
        {
            if (IsOwnKey(key))
                continue;

            foreach (var value in values)
                content.AppendHtml(Hidden(key, value));
        }

        var currentSearch = query[SearchValueName].ToString();
        var currentSize = int.TryParse(query[PageSizeName], out var size) ? size : DefaultPageSize;

        content.AppendHtml(Column("col-12 col-md", SearchBox(currentSearch)));
        content.AppendHtml(childContent);
        content.AppendHtml(Column("col-6 col-md-auto", PageSizeSelect(currentSize)));
        content.AppendHtml(Column("col-6 col-md-auto", SubmitButton()));

        output.Content.SetHtmlContent(content);
    }

    private static bool IsOwnKey(string key) =>
        string.Equals(key, SearchValueName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(key, PageSizeName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(key, PageNumberName, StringComparison.OrdinalIgnoreCase);

    private static TagBuilder Hidden(string name, string? value)
    {
        var input = new TagBuilder("input") { TagRenderMode = TagRenderMode.SelfClosing };
        input.Attributes["type"] = "hidden";
        input.Attributes["name"] = name;
        input.Attributes["value"] = value;
        return input;
    }

    private static TagBuilder Column(string cssClass, IHtmlContent inner)
    {
        var column = new TagBuilder("div");
        column.AddCssClass(cssClass);
        column.InnerHtml.AppendHtml(inner);
        return column;
    }

    private TagBuilder SearchBox(string currentSearch)
    {
        var input = new TagBuilder("input") { TagRenderMode = TagRenderMode.SelfClosing };
        input.AddCssClass("form-control");
        input.Attributes["type"] = "search";
        input.Attributes["name"] = SearchValueName;
        input.Attributes["value"] = currentSearch;
        input.Attributes["placeholder"] = Placeholder;
        input.Attributes["aria-label"] = Placeholder;
        return input;
    }

    private TagBuilder PageSizeSelect(int currentSize)
    {
        var sizes = PageSizes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var n) ? n : 0)
            .Where(x => x > 0)
            .Append(currentSize)
            .Distinct()
            .Order();

        var select = new TagBuilder("select");
        select.AddCssClass("form-select");
        select.Attributes["name"] = PageSizeName;
        select.Attributes["aria-label"] = "Page size";

        foreach (var pageSize in sizes)
        {
            var option = new TagBuilder("option");
            option.Attributes["value"] = pageSize.ToString();
            if (pageSize == currentSize)
                option.Attributes["selected"] = "selected";

            option.InnerHtml.Append($"{pageSize} / page");
            select.InnerHtml.AppendHtml(option);
        }

        return select;
    }

    private static TagBuilder SubmitButton()
    {
        var button = new TagBuilder("button");
        button.AddCssClass("btn btn-primary w-100");
        button.Attributes["type"] = "submit";

        var icon = new TagBuilder("i");
        icon.AddCssClass("bi bi-search me-1");
        icon.Attributes["aria-hidden"] = "true";

        button.InnerHtml.AppendHtml(icon);
        button.InnerHtml.Append("Search");
        return button;
    }
}
