using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Identity.Tests.TestSupport;

/// <summary>
/// Builds the context/output pair a tag helper runs against, outside of a Razor view.
/// </summary>
internal static class TagHelperTestContext
{
    public static TagHelperContext Context() =>
        new(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));

    public static TagHelperOutput Output(string tagName, string childContent = "") =>
        new(
            tagName,
            new TagHelperAttributeList(),
            (_, _) =>
            {
                var content = new DefaultTagHelperContent();
                content.SetContent(childContent);
                return Task.FromResult<TagHelperContent>(content);
            });

    public static ViewContext ViewContext(HttpContext? httpContext = null) =>
        new() { HttpContext = httpContext ?? new DefaultHttpContext() };
}
