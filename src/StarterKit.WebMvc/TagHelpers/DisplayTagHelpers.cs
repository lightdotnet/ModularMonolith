using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace StarterKit.WebMvc.TagHelpers;

/// <summary>
/// Bootstrap Icons glyph: <c>&lt;icon name="trash" /&gt;</c>. Decorative by default; <c>label</c>
/// adds visually hidden text for icon-only controls.
/// </summary>
[HtmlTargetElement("icon", TagStructure = TagStructure.WithoutEndTag)]
public sealed class IconTagHelper : TagHelper
{
    public string Name { get; set; } = string.Empty;

    public string? Label { get; set; }

    public string? Css { get; set; }

    public override void Process(
        TagHelperContext context,
        TagHelperOutput output)
    {
        output.TagName = null;
        output.Content.SetHtmlContent(Render(Name, Css));

        if (!string.IsNullOrEmpty(Label))
        {
            var hidden = new TagBuilder("span");
            hidden.AddCssClass("visually-hidden");
            hidden.InnerHtml.Append(Label);
            output.Content.AppendHtml(hidden);
        }
    }

    public static IHtmlContent Render(
        string name,
        string? cssClass = null)
    {
        var tag = new TagBuilder("i");
        tag.AddCssClass($"bi bi-{name} {cssClass}".Trim());
        tag.Attributes["aria-hidden"] = "true";
        return tag;
    }
}

/// <summary>
/// Status pill: <c>&lt;status-badge value="@user.Status" /&gt;</c>. The Bootstrap variant comes from a
/// small built-in map (active → success, locked → danger, inactive → secondary, …) unless
/// <c>variant</c> is given; <c>text</c> overrides the label. Null/empty values render nothing.
/// </summary>
[HtmlTargetElement("status-badge", TagStructure = TagStructure.WithoutEndTag)]
public sealed class StatusBadgeTagHelper : TagHelper
{
    private static readonly Dictionary<string, string> Variants = new(StringComparer.OrdinalIgnoreCase)
    {
        ["active"] = "success",
        ["enabled"] = "success",
        ["success"] = "success",
        ["approved"] = "success",
        ["inactive"] = "secondary",
        ["disabled"] = "secondary",
        ["locked"] = "danger",
        ["rejected"] = "danger",
        ["failed"] = "danger",
        ["error"] = "danger",
        ["pending"] = "warning",
        ["draft"] = "warning",
    };

    public object? Value { get; set; }

    public string? Text { get; set; }

    public string? Variant { get; set; }

    public override void Process(
        TagHelperContext context,
        TagHelperOutput output)
    {
        var value = Value?.ToString();

        if (string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(Text))
        {
            output.SuppressOutput();
            return;
        }

        var variant = Variant
            ?? (value is not null && Variants.TryGetValue(value, out var mapped) ? mapped : "secondary");

        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", $"badge rounded-pill text-bg-{variant}");
        output.Content.SetContent(Text ?? value);
    }
}

/// <summary>
/// A UTC instant shown in the browser's local time zone: <c>&lt;local-datetime value="@dto.Created" /&gt;</c>.
/// Renders a <c>&lt;time&gt;</c> with the ISO value and a UTC fallback text; <c>local-datetime.js</c>
/// reformats it (<c>format</c>: <c>datetime</c> (default), <c>date</c>, <c>time</c>, <c>relative</c>).
/// Null renders nothing.
/// </summary>
[HtmlTargetElement("local-datetime", TagStructure = TagStructure.WithoutEndTag)]
public sealed class LocalDateTimeTagHelper : TagHelper
{
    public DateTimeOffset? Value { get; set; }

    public string Format { get; set; } = "datetime";

    public override void Process(
        TagHelperContext context,
        TagHelperOutput output)
    {
        if (Value is null)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = null;
        output.Content.SetHtmlContent(Render(
            Value.Value,
            Format));
    }

    public static IHtmlContent Render(
        DateTimeOffset value,
        string format = "datetime")
    {
        var utc = value.ToUniversalTime();
        var tag = new TagBuilder("time");
        tag.Attributes["datetime"] = utc.ToString("o", CultureInfo.InvariantCulture);
        tag.Attributes["data-local-datetime"] = format;
        tag.InnerHtml.Append(utc.ToString(
            format == "date" ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm 'UTC'",
            CultureInfo.InvariantCulture));

        return tag;
    }
}

/// <summary>
/// A formatted number, right-aligned-friendly (<c>numeric</c> class, tabular digits):
/// <c>&lt;number value="@amount" /&gt;</c> uses <c>#,##0.00</c>; <c>format</c> overrides it
/// (e.g. <c>#,##0</c> for whole-number counts). Null renders nothing.
/// </summary>
[HtmlTargetElement("number", TagStructure = TagStructure.WithoutEndTag)]
public sealed class NumberTagHelper : TagHelper
{
    public const string DefaultFormat = "#,##0.00";

    public object? Value { get; set; }

    public string Format { get; set; } = DefaultFormat;

    public override void Process(
        TagHelperContext context,
        TagHelperOutput output)
    {
        if (Value is not IFormattable formattable)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "numeric");
        output.Content.SetContent(formattable.ToString(
            Format,
            CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Empty-state block: <c>&lt;empty-state icon="inbox" title="No users found" description="Try adjusting your search." /&gt;</c>;
/// child content (e.g. a create button) renders below the text.
/// </summary>
[HtmlTargetElement("empty-state")]
public sealed class EmptyStateTagHelper : TagHelper
{
    public string Icon { get; set; } = "inbox";

    public string Title { get; set; } = "No records";

    public string? Description { get; set; }

    public override async Task ProcessAsync(
        TagHelperContext context,
        TagHelperOutput output)
    {
        var child = await output.GetChildContentAsync();

        output.TagName = null;
        output.Content.SetHtmlContent(Render(
            Icon,
            Title,
            Description,
            child.IsEmptyOrWhiteSpace ? null : child));
    }

    public static IHtmlContent Render(
        string icon,
        string title,
        string? description,
        IHtmlContent? actions = null)
    {
        var wrapper = new TagBuilder("div");
        wrapper.AddCssClass("empty-state text-center text-body-secondary py-5 px-3");

        var media = new TagBuilder("div");
        media.AddCssClass("empty-state-icon mb-2");
        media.InnerHtml.AppendHtml(IconTagHelper.Render(icon, "fs-2"));
        wrapper.InnerHtml.AppendHtml(media);

        var heading = new TagBuilder("div");
        heading.AddCssClass("fw-semibold text-body");
        heading.InnerHtml.Append(title);
        wrapper.InnerHtml.AppendHtml(heading);

        if (!string.IsNullOrEmpty(description))
        {
            var text = new TagBuilder("div");
            text.AddCssClass("small");
            text.InnerHtml.Append(description);
            wrapper.InnerHtml.AppendHtml(text);
        }

        if (actions is not null)
        {
            var actionsWrapper = new TagBuilder("div");
            actionsWrapper.AddCssClass("mt-3");
            actionsWrapper.InnerHtml.AppendHtml(actions);
            wrapper.InnerHtml.AppendHtml(actionsWrapper);
        }

        return wrapper;
    }
}

internal static class HtmlContentExtensions
{
    public static string ToHtmlString(this IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(
            writer,
            HtmlEncoder.Default);

        return writer.ToString();
    }
}
