namespace StarterKit.WebMvc.Services.Http;

/// <summary>
/// Raised when the backend answers 401 or 403 to an authenticated call. Handled centrally by
/// <see cref="Infrastructure.ApiAuthorizationExceptionMiddleware"/>: 401 signs the user out and sends them to
/// the login page, 403 sends them to the access-denied page (or returns the bare status code to
/// a fetch request).
/// </summary>
public sealed class ApiAuthorizationException(
    int statusCode,
    string message)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
