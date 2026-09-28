namespace StarterKit.WebMvc.Services.Http;

/// <summary>
/// Outcome of one backend call, normalized from the backend's <c>Result</c>/<c>ApiResponse</c>
/// envelope (or from a transport failure) so callers only ever handle one shape — the same
/// role <c>guardCall</c> plays in the admin client. 401/403 normally never reach a caller as a
/// result: they are raised as <see cref="ApiAuthorizationException"/> (see
/// <see cref="ApiRequest.ThrowOnAuthFailure"/>).
/// </summary>
public class ApiResult
{
    private static readonly IReadOnlyDictionary<string, string[]> NoValidationErrors =
        new Dictionary<string, string[]>();

    public bool IsSuccess { get; init; }

    /// <summary>Backend result code (<c>success</c>, <c>bad_request</c>, <c>unauthorized</c>, ...).</summary>
    public string Code { get; init; } = ApiResultCodes.Error;

    public string Message { get; init; } = string.Empty;

    /// <summary>HTTP status of the backend response; <c>null</c> when the request never got one (network/timeout).</summary>
    public int? StatusCode { get; init; }

    public string? RequestId { get; init; }

    /// <summary>Field-level validation errors, keyed by the backend's property path.</summary>
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; init; } = NoValidationErrors;

    public bool HasValidationErrors => ValidationErrors.Count > 0;

    /// <summary>
    /// True for failures that say nothing about the request itself (network, timeout, 5xx) —
    /// callers such as the session refresh must not treat them as a verdict on the token.
    /// </summary>
    public bool IsTransient => !IsSuccess && (StatusCode is null || StatusCode >= 500);

    public static ApiResult<T> TransportFailure<T>(
        string message)
    {
        return new ApiResult<T>
        {
            IsSuccess = false,
            Code = ApiResultCodes.Error,
            Message = message,
        };
    }
}

/// <inheritdoc cref="ApiResult"/>
public sealed class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }
}

/// <summary>
/// The backend's result-code vocabulary (<c>Light.Contracts.ResultCode</c> names).
/// </summary>
public static class ApiResultCodes
{
    public const string Success = "success";

    public const string BadRequest = "bad_request";

    public const string Unauthorized = "unauthorized";

    public const string Forbidden = "forbidden";

    public const string NotFound = "not_found";

    public const string Conflict = "conflict";

    public const string Error = "error";

    public static string FromStatusCode(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => BadRequest,
        StatusCodes.Status401Unauthorized => Unauthorized,
        StatusCodes.Status403Forbidden => Forbidden,
        StatusCodes.Status404NotFound => NotFound,
        StatusCodes.Status409Conflict => Conflict,
        _ when statusCode is >= 200 and < 300 => Success,
        _ => Error,
    };
}
