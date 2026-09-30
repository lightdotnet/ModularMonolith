using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace StarterKit.Modules.Identity.Web.TagHelpers;

/// <summary>
/// <c>&lt;form-field asp-for="Input.Email" /&gt;</c> renders a Bootstrap form group — label,
/// control and validation message — through the built-in <see cref="IHtmlGenerator"/>:
/// a checkbox for <see cref="bool"/>, a select when <c>asp-items</c> is set, a textarea for
/// <c>DataType.MultilineText</c>, otherwise an input whose type follows the
/// <c>[DataType]</c> metadata (password, email, tel).
/// </summary>
[HtmlTargetElement("form-field", Attributes = ForAttributeName, TagStructure = TagStructure.WithoutEndTag)]
public sealed class FormFieldTagHelper(IHtmlGenerator generator) : TagHelper
{
    private const string ForAttributeName = "asp-for";

    [HtmlAttributeName(ForAttributeName)]
    public ModelExpression For { get; set; } = null!;

    [HtmlAttributeName("asp-items")]
    public IEnumerable<SelectListItem>? Items { get; set; }

    /// <summary>
    /// Label text; the model metadata display name when omitted.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Empty first option of a select (e.g. "Select a role").
    /// </summary>
    public string? OptionLabel { get; set; }

    public string? Placeholder { get; set; }

    public string? Autocomplete { get; set; }

    /// <summary>
    /// Help text shown under the control.
    /// </summary>
    public string? Hint { get; set; }

    public bool Readonly { get; set; }

    /// <summary>
    /// Renders the control disabled (a disabled control is not posted).
    /// </summary>
    public bool Disabled { get; set; }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var isCheckbox = Items is null
            && (For.Metadata.UnderlyingOrModelType == typeof(bool));

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;

        var existingClass = output.Attributes["class"]?.Value?.ToString();
        var groupClass = isCheckbox ? "form-check mb-3" : "mb-3";
        output.Attributes.SetAttribute(
            "class",
            string.IsNullOrWhiteSpace(existingClass) ? groupClass : $"{groupClass} {existingClass}");

        var content = new HtmlContentBuilder();

        if (isCheckbox)
        {
            content.AppendHtml(Checkbox());
            content.AppendHtml(generator.GenerateHiddenForCheckbox(ViewContext, For.ModelExplorer, For.Name));
            content.AppendHtml(LabelFor("form-check-label"));
        }
        else
        {
            content.AppendHtml(LabelFor("form-label"));
            content.AppendHtml(Control());
        }

        if (!string.IsNullOrWhiteSpace(Hint))
        {
            var hint = new TagBuilder("div");
            hint.AddCssClass("form-text");
            hint.InnerHtml.Append(Hint);
            content.AppendHtml(hint);
        }

        content.AppendHtml(generator.GenerateValidationMessage(
            ViewContext,
            For.ModelExplorer,
            For.Name,
            message: null,
            tag: "div",
            htmlAttributes: new Dictionary<string, object> { ["class"] = "invalid-feedback d-block" }));

        output.Content.SetHtmlContent(content);
    }

    private TagBuilder LabelFor(string cssClass) =>
        generator.GenerateLabel(
            ViewContext,
            For.ModelExplorer,
            For.Name,
            labelText: Label,
            htmlAttributes: new Dictionary<string, object> { ["class"] = cssClass });

    private TagBuilder Checkbox() =>
        generator.GenerateCheckBox(
            ViewContext,
            For.ModelExplorer,
            For.Name,
            isChecked: null,
            htmlAttributes: ControlAttributes("form-check-input"));

    private TagBuilder Control()
    {
        if (Items is not null)
        {
            return generator.GenerateSelect(
                ViewContext,
                For.ModelExplorer,
                optionLabel: OptionLabel,
                expression: For.Name,
                selectList: Items,
                allowMultiple: false,
                htmlAttributes: ControlAttributes("form-select"));
        }

        var dataType = For.Metadata.DataTypeName;

        if (dataType == "MultilineText")
        {
            return generator.GenerateTextArea(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                rows: 3,
                columns: 0,
                htmlAttributes: ControlAttributes("form-control"));
        }

        if (dataType == "Password")
        {
            return generator.GeneratePassword(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                value: null,
                htmlAttributes: ControlAttributes("form-control"));
        }

        var input = generator.GenerateTextBox(
            ViewContext,
            For.ModelExplorer,
            For.Name,
            value: For.Model,
            format: null,
            htmlAttributes: ControlAttributes("form-control"));

        input.Attributes["type"] = dataType switch
        {
            "EmailAddress" => "email",
            "PhoneNumber" => "tel",
            "Url" => "url",
            _ => "text",
        };

        return input;
    }

    private Dictionary<string, object> ControlAttributes(string cssClass)
    {
        var attributes = new Dictionary<string, object>
        {
            ["class"] = HasErrors() ? $"{cssClass} is-invalid" : cssClass,
        };

        if (!string.IsNullOrWhiteSpace(Placeholder))
            attributes["placeholder"] = Placeholder;

        if (!string.IsNullOrWhiteSpace(Autocomplete))
            attributes["autocomplete"] = Autocomplete;

        if (Readonly)
            attributes["readonly"] = "readonly";

        if (Disabled)
            attributes["disabled"] = "disabled";

        return attributes;
    }

    private bool HasErrors()
    {
        var key = ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(For.Name);

        return ViewContext.ViewData.ModelState.TryGetValue(key, out var entry)
            && entry.Errors.Count > 0;
    }
}
