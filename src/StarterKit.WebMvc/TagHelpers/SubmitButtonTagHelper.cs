using Microsoft.AspNetCore.Razor.TagHelpers;

namespace StarterKit.WebMvc.TagHelpers;

/// <summary>
/// Submit button with a spinner shown while the form submits (site.js locks the form once a
/// submit actually proceeds past client validation):
/// <code>&lt;submit-button icon="check-lg"&gt;Save&lt;/submit-button&gt;</code>
/// </summary>
[HtmlTargetElement("submit-button")]
public sealed class SubmitButtonTagHelper : TagHelper
{
    /// <summary>Bootstrap button variant (<c>primary</c>, <c>danger</c>, <c>outline-secondary</c>, …).</summary>
    public string Variant { get; set; } = "primary";

    public string? Icon { get; set; }

    /// <summary>Extra CSS classes (e.g. <c>w-100</c>).</summary>
    public string? Css { get; set; }

    public string? Name { get; set; }

    public string? Value { get; set; }

    /// <summary>Renders the button disabled (e.g. while the form cannot be saved safely).</summary>
    public bool Disabled { get; set; }

    public override async Task ProcessAsync(
        TagHelperContext context,
        TagHelperOutput output)
    {
        var label = await output.GetChildContentAsync();

        output.TagName = "button";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("type", "submit");
        output.Attributes.SetAttribute("class", $"btn btn-{Variant} {Css}".Trim());
        output.Attributes.SetAttribute("data-submit-button", string.Empty);

        if (Disabled)
        {
            output.Attributes.SetAttribute("disabled", "disabled");
        }

        if (!string.IsNullOrEmpty(Name))
        {
            output.Attributes.SetAttribute("name", Name);
            output.Attributes.SetAttribute("value", Value ?? string.Empty);
        }

        output.Content.AppendHtml("<span class=\"spinner-border spinner-border-sm me-2 d-none\" aria-hidden=\"true\" data-loading-indicator></span>");

        if (!string.IsNullOrEmpty(Icon))
        {
            output.Content.AppendHtml(IconTagHelper.Render(Icon, "me-2"));
        }

        output.Content.AppendHtml(label);
    }
}
