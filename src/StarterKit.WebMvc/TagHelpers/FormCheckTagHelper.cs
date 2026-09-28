using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace StarterKit.WebMvc.TagHelpers;

/// <summary>
/// Bootstrap checkbox (or <c>switch="true"</c> toggle) bound to a <c>bool</c> property:
/// <code>&lt;form-check asp-for="Input.IsActive" label="Active" switch="true" /&gt;</code>
/// </summary>
[HtmlTargetElement("form-check", TagStructure = TagStructure.WithoutEndTag)]
public sealed class FormCheckTagHelper(
    IHtmlGenerator generator)
    : TagHelper
{
    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = null!;

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public string? Label { get; set; }

    public string? Help { get; set; }

    public bool Switch { get; set; }

    public bool Disabled { get; set; }

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
            { "class", "form-check-input" },
        };

        if (Switch)
        {
            attributes.Add(
                "role",
                "switch");
        }

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

        // InputTagHelper renders the checkbox and appends the hidden "false" input to the form end.
        var input = await TagHelperComposer.RunAsync(
            new InputTagHelper(generator) { For = For, ViewContext = ViewContext, InputTypeName = "checkbox" },
            "input",
            TagMode.SelfClosing,
            attributes);

        var label = await FormFieldParts.LabelAsync(
            generator,
            ViewContext,
            For,
            Label,
            required: false,
            "form-check-label");

        var wrapper = new TagBuilder("div");
        wrapper.AddCssClass(Switch ? "form-check form-switch" : "form-check");
        wrapper.InnerHtml.AppendHtml(input);
        wrapper.InnerHtml.AppendHtml(label);
        wrapper.InnerHtml.AppendHtml(await FormFieldParts.ValidationMessageAsync(
            generator,
            ViewContext,
            For));

        wrapper.InnerHtml.AppendHtml(FormFieldParts.HelpText(
            Help,
            helpId));

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", Css);
        output.Content.SetHtmlContent(wrapper);
    }
}
