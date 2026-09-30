using Microsoft.AspNetCore.Razor.TagHelpers;

namespace StarterKit.Modules.Identity.Web.TagHelpers;

/// <summary>
/// Renders the element only when the feature named in <c>asp-feature</c> is enabled.
/// </summary>
/// <remarks>
/// Runs after the other tag helpers on the same element so its suppression is final.
/// </remarks>
[HtmlTargetElement(Attributes = AttributeName)]
public sealed class FeatureTagHelper(IFeatureToggle featureToggle) : TagHelper
{
    public const string AttributeName = "asp-feature";

    [HtmlAttributeName(AttributeName)]
    public string? Feature { get; set; }

    public override int Order => 1000;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(Feature) || !featureToggle.IsEnabled(Feature))
            output.SuppressOutput();
    }
}
