using Microsoft.AspNetCore.Razor.TagHelpers;

namespace StarterKit.Modules.Identity.Web.TagHelpers;

/// <summary>
/// <c>&lt;status-badge value="Active" /&gt;</c> renders a Bootstrap badge whose color follows
/// the status text. An empty value renders nothing.
/// </summary>
[HtmlTargetElement("status-badge", TagStructure = TagStructure.WithoutEndTag)]
public sealed class StatusBadgeTagHelper : TagHelper
{
    public string? Value { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(Value))
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", $"badge {ColorOf(Value)}");
        output.Content.SetContent(Value);
    }

    public static string ColorOf(string value) => value.Trim().ToLowerInvariant() switch
    {
        "active" or "enabled" or "success" or "approved" => "text-bg-success",
        "inactive" or "disabled" or "draft" => "text-bg-secondary",
        "locked" or "blocked" or "deleted" or "error" or "failed" or "rejected" => "text-bg-danger",
        "pending" or "warning" => "text-bg-warning",
        _ => "text-bg-light",
    };
}
