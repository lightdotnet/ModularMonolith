using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace StarterKit.WebMvc.TagHelpers;

/// <summary>
/// Label + input (or textarea) + validation message + optional help text, Bootstrap-styled, with a
/// required marker from the model metadata:
/// <code>&lt;form-field asp-for="Input.Email" label="Email" help="Used for sign-in notices." /&gt;</code>
/// <c>type</c> overrides the inferred input type (<c>textarea</c> renders a textarea with <c>rows</c>).
/// </summary>
[HtmlTargetElement("form-field", TagStructure = TagStructure.WithoutEndTag)]
public sealed class FormFieldTagHelper(
    IHtmlGenerator generator)
    : TagHelper
{
    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = null!;

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    /// <summary>Label text; defaults to the model's display name.</summary>
    public string? Label { get; set; }

    public string? Help { get; set; }

    /// <summary>Input type override (<c>text</c>, <c>email</c>, <c>password</c>, <c>number</c>, <c>textarea</c>, …).</summary>
    public string? Type { get; set; }

    public string? Placeholder { get; set; }

    public int Rows { get; set; } = 3;

    public string? Autocomplete { get; set; }

    public bool Disabled { get; set; }

    public bool Readonly { get; set; }

    public bool Autofocus { get; set; }

    /// <summary>Overrides the required marker (defaults to the model metadata).</summary>
    public bool? Required { get; set; }

    /// <summary>Wrapper CSS classes (default <c>mb-3</c>; add grid classes such as <c>col-md-6</c>).</summary>
    public string Css { get; set; } = "mb-3";

    /// <summary>Extra classes for the input element.</summary>
    public string? InputCss { get; set; }

    /// <summary>Extra attributes copied onto the input, e.g. <c>input-attr-list="users"</c>.</summary>
    [HtmlAttributeName("input-attr-all", DictionaryAttributePrefix = "input-attr-")]
    public IDictionary<string, string?> InputAttributes { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    public override async Task ProcessAsync(
        TagHelperContext context,
        TagHelperOutput output)
    {
        var isTextArea = string.Equals(Type, "textarea", StringComparison.OrdinalIgnoreCase);
        var inputId = TagBuilder.CreateSanitizedId(
            For.Name,
            "_");

        var helpId = string.IsNullOrEmpty(Help) ? null : $"{inputId}-help";

        var inputAttributes = new TagHelperAttributeList
        {
            { "class", $"form-control {InputCss}".Trim() },
        };

        AddOptional(inputAttributes, "placeholder", Placeholder);
        AddOptional(inputAttributes, "autocomplete", Autocomplete);
        AddOptional(inputAttributes, "aria-describedby", helpId);

        if (Disabled)
        {
            inputAttributes.Add("disabled", "disabled");
        }

        if (Readonly)
        {
            inputAttributes.Add("readonly", "readonly");
        }

        if (Autofocus)
        {
            inputAttributes.Add("autofocus", "autofocus");
        }

        foreach (var (name, value) in InputAttributes)
        {
            inputAttributes.SetAttribute(name, value ?? string.Empty);
        }

        TagHelperOutput input;

        if (isTextArea)
        {
            inputAttributes.Add("rows", Rows);

            input = await TagHelperComposer.RunAsync(
                new TextAreaTagHelper(generator) { For = For, ViewContext = ViewContext },
                "textarea",
                TagMode.StartTagAndEndTag,
                inputAttributes);
        }
        else
        {
            input = await TagHelperComposer.RunAsync(
                new InputTagHelper(generator) { For = For, ViewContext = ViewContext, InputTypeName = Type },
                "input",
                TagMode.SelfClosing,
                inputAttributes);
        }

        var content = await FormFieldParts.WrapAsync(
            generator,
            ViewContext,
            For,
            Label,
            Required ?? For.Metadata.IsRequired,
            Help,
            helpId,
            input);

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", Css);
        output.Content.SetHtmlContent(content);
    }

    private static void AddOptional(
        TagHelperAttributeList attributes,
        string name,
        string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            attributes.Add(
                name,
                value);
        }
    }
}

/// <summary>Label / validation message / help text shared by the form tag helpers.</summary>
internal static class FormFieldParts
{
    public static async Task<IHtmlContent> WrapAsync(
        IHtmlGenerator generator,
        ViewContext viewContext,
        ModelExpression @for,
        string? label,
        bool required,
        string? help,
        string? helpId,
        IHtmlContent control)
    {
        var builder = new HtmlContentBuilder();

        builder.AppendHtml(await LabelAsync(
            generator,
            viewContext,
            @for,
            label,
            required,
            "form-label"));

        builder.AppendHtml(control);
        builder.AppendHtml(await ValidationMessageAsync(
            generator,
            viewContext,
            @for));

        builder.AppendHtml(HelpText(
            help,
            helpId));

        return builder;
    }

    public static async Task<IHtmlContent> LabelAsync(
        IHtmlGenerator generator,
        ViewContext viewContext,
        ModelExpression @for,
        string? label,
        bool required,
        string cssClass)
    {
        var text = new HtmlContentBuilder();
        text.Append(label ?? @for.Metadata.GetDisplayName());

        if (required)
        {
            text.AppendHtml("<span class=\"text-danger ms-1\" aria-hidden=\"true\">*</span>");
        }

        return await TagHelperComposer.RunAsync(
            new LabelTagHelper(generator) { For = @for, ViewContext = viewContext },
            "label",
            TagMode.StartTagAndEndTag,
            new TagHelperAttributeList { { "class", cssClass } },
            text);
    }

    public static async Task<IHtmlContent> ValidationMessageAsync(
        IHtmlGenerator generator,
        ViewContext viewContext,
        ModelExpression @for)
    {
        return await TagHelperComposer.RunAsync(
            new ValidationMessageTagHelper(generator) { For = @for, ViewContext = viewContext },
            "span",
            TagMode.StartTagAndEndTag,
            new TagHelperAttributeList { { "class", "invalid-feedback d-block" } });
    }

    public static IHtmlContent HelpText(
        string? help,
        string? helpId)
    {
        if (string.IsNullOrEmpty(help))
        {
            return HtmlString.Empty;
        }

        var tag = new TagBuilder("div");
        tag.AddCssClass("form-text");
        tag.Attributes["id"] = helpId;
        tag.InnerHtml.Append(help);
        return tag;
    }
}
