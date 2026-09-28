namespace StarterKit.WebMvc.Services.Http;

/// <summary>
/// Named <see cref="HttpClient"/>s — one per backend module, each bound to that module's
/// configured base URL (<c>Api:&lt;Module&gt;:BaseUrl</c>).
/// </summary>
internal static class ApiClientNames
{
    public const string Identity = "Identity";

    public const string Notifications = "Notifications";
}
