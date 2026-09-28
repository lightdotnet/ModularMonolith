using System.Text.Json;
using System.Text.Json.Serialization;

namespace StarterKit.WebMvc.Services.Http;

/// <summary>
/// JSON settings matching the backend's wire format: camelCase, case-insensitive reads, and
/// enums serialized by name (the backend registers <see cref="JsonStringEnumConverter"/>).
/// </summary>
internal static class ApiJson
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
