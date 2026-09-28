using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace StarterKit.WebMvc.TagHelpers;

/// <summary>
/// Runs a framework tag helper (input/label/select/validation-message, …) programmatically so the
/// composite form tag helpers reuse its exact behaviour — input type inference, <c>data-val-*</c>
/// attributes, model-state values — instead of re-implementing it.
/// </summary>
internal static class TagHelperComposer
{
    public static async Task<TagHelperOutput> RunAsync(
        ITagHelper tagHelper,
        string tagName,
        TagMode tagMode,
        TagHelperAttributeList? attributes = null,
        IHtmlContent? childContent = null)
    {
        var context = new TagHelperContext(
            tagName,
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));

        var output = new TagHelperOutput(
            tagName,
            attributes ?? [],
            (_, _) =>
            {
                var content = new DefaultTagHelperContent();

                if (childContent is not null)
                {
                    content.SetHtmlContent(childContent);
                }

                return Task.FromResult<TagHelperContent>(content);
            })
        {
            TagMode = tagMode,
        };

        tagHelper.Init(context);

        await tagHelper.ProcessAsync(
            context,
            output);

        // Mirrors the Razor runtime: a helper that leaves Content untouched renders its child content.
        if (!output.IsContentModified && childContent is not null)
        {
            output.Content.SetHtmlContent(childContent);
        }

        return output;
    }

    public static void AddClass(
        TagHelperOutput output,
        string? cssClass)
    {
        if (string.IsNullOrWhiteSpace(cssClass))
        {
            return;
        }

        var existing = output.Attributes.TryGetAttribute("class", out var attribute)
            ? attribute.Value?.ToString()
            : null;

        output.Attributes.SetAttribute(
            "class",
            string.IsNullOrWhiteSpace(existing) ? cssClass : $"{existing} {cssClass}");
    }
}
