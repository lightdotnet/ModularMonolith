using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace StarterKit.Modules.Identity.Web.TagHelpers;

/// <summary>
/// <c>&lt;confirm-post asp-page-handler="Delete" asp-route-id="@x.Id" message="Delete?" icon="trash" /&gt;</c>
/// renders a POST form (with the antiforgery token) holding one button. The button carries
/// <c>data-confirm</c>, so the shared confirm modal script asks before the form is submitted.
/// </summary>
[HtmlTargetElement("confirm-post", TagStructure = TagStructure.WithoutEndTag)]
public sealed class ConfirmPostTagHelper(
    IUrlHelperFactory urlHelperFactory,
    IHtmlGenerator htmlGenerator)
    : TagHelper
{
    private const string RouteAttributePrefix = "asp-route-";

    /// <summary>
    /// Target page; the current page when omitted.
    /// </summary>
    [HtmlAttributeName("asp-page")]
    public string? Page { get; set; }

    [HtmlAttributeName("asp-page-handler")]
    public string? PageHandler { get; set; }

    [HtmlAttributeName(DictionaryAttributePrefix = RouteAttributePrefix)]
    public IDictionary<string, string?> RouteValues { get; set; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Question shown in the confirm modal.
    /// </summary>
    public string Message { get; set; } = "Are you sure?";

    /// <summary>
    /// Button tooltip and accessible name.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Optional Bootstrap Icons name (without the <c>bi-</c> prefix).
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Optional visible button text; the button is icon-only without it.
    /// </summary>
    public string? Text { get; set; }

    public string ButtonClass { get; set; } = "btn btn-sm btn-outline-danger";

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var urlHelper = urlHelperFactory.GetUrlHelper(ViewContext);

        var values = RouteValues.ToDictionary(x => x.Key, x => (object?)x.Value);
        var action = urlHelper.Page(Page, PageHandler, values);

        output.TagName = "form";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("method", "post");
        output.Attributes.SetAttribute("action", action);

        if (!output.Attributes.ContainsName("class"))
            output.Attributes.SetAttribute("class", "d-inline");

        var button = new TagBuilder("button");
        button.Attributes["type"] = "submit";
        button.Attributes["data-confirm"] = Message;
        button.AddCssClass(ButtonClass);

        var label = Title ?? Text;
        if (!string.IsNullOrWhiteSpace(label))
        {
            button.Attributes["title"] = label;
            button.Attributes["aria-label"] = label;
        }

        if (!string.IsNullOrWhiteSpace(Icon))
        {
            var icon = new TagBuilder("i");
            icon.AddCssClass($"bi bi-{Icon}");
            icon.Attributes["aria-hidden"] = "true";
            button.InnerHtml.AppendHtml(icon);
        }

        if (!string.IsNullOrWhiteSpace(Text))
        {
            if (!string.IsNullOrWhiteSpace(Icon))
                button.InnerHtml.Append(" ");

            button.InnerHtml.Append(Text);
        }

        output.Content.AppendHtml(htmlGenerator.GenerateAntiforgery(ViewContext));
        output.Content.AppendHtml(button);
    }
}
