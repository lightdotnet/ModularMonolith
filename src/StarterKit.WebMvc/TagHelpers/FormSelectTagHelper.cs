using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace StarterKit.WebMvc.TagHelpers;

/// <summary>
/// Label + select + validation message + help text:
/// <code>&lt;form-select asp-for="Input.Status" asp-items="Model.StatusOptions" placeholder="Select a status" /&gt;</code>
/// <c>placeholder</c> adds a first, empty-valued option.
/// </summary>
[HtmlTargetElement("form-select", TagStructure = TagStructure.WithoutEndTag)]
public sealed class FormSelectTagHelper(
    IHtmlGenerator generator)
    : TagHelper
{
    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = null!;

    [HtmlAttributeName("asp-items")]
    public IEnumerable<SelectListItem> Items { get; set; } = [];

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public string? Label { get; set; }

    public string? Help { get; set; }

    public string? Placeholder { get; set; }

    public bool Disabled { get; set; }

    public bool? Required { get; set; }

    public string Css { get; set; } = "mb-3";

    public override async Task ProcessAsync(
        TagHelperContext context,
        TagHelperOutput output)
    {
        var inputId = TagBuilder.CreateSanitizedId(
            For.Name,
            "_");

        var helpId = string.IsNullOrEmpty(Help) ? null : $"{inputId}-help";

        var attributes = new TagHelperAttributeList
        {
            { "class", "form-select" },
        };

        if (helpId is not null)
        {
            attributes.Add(
                "aria-describedby",
                helpId);
        }

        if (Disabled)
        {
            attributes.Add(
                "disabled",
                "disabled");
        }

        var options = new HtmlContentBuilder();

        if (Placeholder is not null)
        {
            var option = new TagBuilder("option");
            option.Attributes["value"] = string.Empty;
            option.InnerHtml.Append(Placeholder);
            options.AppendHtml(option);
        }

        var select = await TagHelperComposer.RunAsync(
            new SelectTagHelper(generator) { For = For, Items = Items, ViewContext = ViewContext },
            "select",
            TagMode.StartTagAndEndTag,
            attributes,
            options);

        var content = await FormFieldParts.WrapAsync(
            generator,
            ViewContext,
            For,
            Label,
            Required ?? For.Metadata.IsRequired,
            Help,
            helpId,
            select);

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", Css);
        output.Content.SetHtmlContent(content);
    }
}
