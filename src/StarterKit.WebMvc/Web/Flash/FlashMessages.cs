using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace StarterKit.WebMvc.Web.Flash;

public enum FlashType
{
    Success,
    Error,
    Warning,
    Info,
}

public sealed record FlashMessage(
    string Type,
    string Message);

/// <summary>
/// One-shot messages carried across a redirect in TempData (post/redirect/get), rendered by the
/// <c>Toasts</c> view component and shown through the site.js toast handler.
/// </summary>
public static class FlashMessages
{
    private const string TempDataKey = "__flash";

    public static void AddFlash(
        this ITempDataDictionary tempData,
        FlashType type,
        string message)
    {
        var messages = Peek(tempData).ToList();

        messages.Add(new FlashMessage(
            ToVariant(type),
            message));

        tempData[TempDataKey] = JsonSerializer.Serialize(messages);
    }

    /// <summary>Reads and consumes the pending messages.</summary>
    public static IReadOnlyList<FlashMessage> Take(ITempDataDictionary tempData)
    {
        var messages = Peek(tempData);
        tempData.Remove(TempDataKey);
        return messages;
    }

    private static IReadOnlyList<FlashMessage> Peek(ITempDataDictionary tempData)
    {
        if (tempData.Peek(TempDataKey) is not string json || string.IsNullOrEmpty(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<FlashMessage>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>Maps to the Bootstrap contextual name the toast handler expects.</summary>
    private static string ToVariant(FlashType type) => type switch
    {
        FlashType.Success => "success",
        FlashType.Error => "danger",
        FlashType.Warning => "warning",
        _ => "info",
    };
}
