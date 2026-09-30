namespace StarterKit.Modules.Identity.Web.TagHelpers;

/// <summary>
/// Answers whether a named UI feature is enabled; consumed by the <c>asp-feature</c> tag helper.
/// </summary>
public interface IFeatureToggle
{
    bool IsEnabled(string feature);
}
