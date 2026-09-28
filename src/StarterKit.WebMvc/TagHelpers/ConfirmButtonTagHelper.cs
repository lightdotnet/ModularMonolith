using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace StarterKit.WebMvc.TagHelpers;

/// <summary>
/// A button that opens the shared confirm modal (<c>_ConfirmModal</c>) and, on confirmation, POSTs
/// to the target with the antiforgery token and the current page URL as <c>returnUrl</c>:
/// <code>
/// &lt;confirm-button asp-page-handler="Delete" asp-route-id="@row.Item.Id"
///                 title="Delete user" message="Delete this user?" confirm-text="Delete"
///                 variant="danger" icon="trash" icon-only="true"&gt;Delete&lt;/confirm-button&gt;
/// </code>
/// Target: <c>asp-page</c>/<c>asp-page-handler</c> (Razor Pages) or <c>asp-controller</c>/<c>asp-action</c>
/// (MVC) plus <c>asp-route-*</c>, or a literal <c>href</c>. With <c>form="id"</c> it submits that
/// existing form instead (so typed inputs are posted too).
/// </summary>
[HtmlTargetElement("confirm-button")]
public sealed class ConfirmButtonTagHelper(
    IUrlHelperFactory urlHelperFactory)
    : TagHelper
{
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    [HtmlAttributeName("asp-page")]
    public string? Page { get; set; }

    [HtmlAttributeName("asp-page-handler")]
    public string? PageHandler { get; set; }

    [HtmlAttributeName("asp-controller")]
    public string? Controller { get; set; }

    [HtmlAttributeName("asp-action")]
    public string? Action { get; set; }

    [HtmlAttributeName("asp-all-route-data", DictionaryAttributePrefix = "asp-route-")]
    public IDictionary<string, string?> RouteValues { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    public string? Href { get; set; }

    /// <summary>Id of an existing form to submit on confirmation.</summary>
    public string? Form { get; set; }

    public string Title { get; set; } = "Are you sure?";

    public string? Message { get; set; }

    public string ConfirmText { get; set; } = "Confirm";

    /// <summary>Bootstrap variant of both the trigger and the modal's confirm button.</summary>
    public string Variant { get; set; } = "danger";

    /// <summary>Trigger style; defaults to <c>btn-{variant}</c>, or <c>btn-outline-{variant} btn-sm</c> when icon-only.</summary>
    public string? Css { get; set; }

    public string? Icon { get; set; }

    /// <summary>Renders only the icon; the child content becomes the accessible label/tooltip.</summary>
    public bool IconOnly { get; set; }

    public override async Task ProcessAsync(
        TagHelperContext context,
        TagHelperOutput output)
    {
        var label = (await output.GetChildContentAsync()).GetContent().Trim();

        output.TagName = "button";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("type", "button");
        output.Attributes.SetAttribute(
            "class",
            Css ?? (IconOnly ? $"btn btn-sm btn-outline-{Variant}" : $"btn btn-{Variant}"));

        output.Attributes.SetAttribute("data-confirm", string.Empty);
        output.Attributes.SetAttribute("data-confirm-title", Title);
        output.Attributes.SetAttribute("data-confirm-text", ConfirmText);
        output.Attributes.SetAttribute("data-confirm-variant", Variant);

        if (!string.IsNullOrEmpty(Message))
        {
            output.Attributes.SetAttribute("data-confirm-message", Message);
        }

        if (!string.IsNullOrEmpty(Form))
        {
            output.Attributes.SetAttribute("data-confirm-form", Form);
        }
        else
        {
            output.Attributes.SetAttribute("data-confirm-url", ResolveUrl());
        }

        if (!string.IsNullOrEmpty(Icon))
        {
            output.Content.AppendHtml(IconTagHelper.Render(Icon, IconOnly ? null : "me-2"));
        }

        if (IconOnly)
        {
            output.Attributes.SetAttribute("title", label);
            output.Attributes.SetAttribute("aria-label", label);
        }
        else
        {
            output.Content.Append(label);
        }
    }

    private string ResolveUrl()
    {
        if (!string.IsNullOrEmpty(Href))
        {
            return Href;
        }

        var urlHelper = urlHelperFactory.GetUrlHelper(ViewContext);
        var values = RouteValues.ToDictionary(
            pair => pair.Key,
            pair => (object?)pair.Value);

        var url = Page is not null || PageHandler is not null
            ? urlHelper.Page(
                Page ?? ViewContext.RouteData.Values["page"] as string,
                PageHandler,
                values)
            : urlHelper.Action(
                Action,
                Controller,
                values);

        return url ?? throw new InvalidOperationException("confirm-button could not resolve its target URL.");
    }
}
