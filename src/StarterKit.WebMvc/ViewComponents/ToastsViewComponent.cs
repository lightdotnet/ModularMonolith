using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StarterKit.WebMvc.Web.Flash;

namespace StarterKit.WebMvc.ViewComponents;

/// <summary>
/// Hands pending TempData flash messages to the browser as a JSON data block; <c>toast.js</c> shows
/// them through the site toast handler on page load.
/// </summary>
public sealed class ToastsViewComponent : ViewComponent
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public IViewComponentResult Invoke()
    {
        var messages = FlashMessages.Take(TempData);

        // The default encoder escapes <, > and & as \uXXXX, so the JSON is safe inside <script>.
        return View(
            "Default",
            messages.Count == 0 ? null : JsonSerializer.Serialize(messages, JsonOptions));
    }
}
