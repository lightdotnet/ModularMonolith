using System.Text.Json;
using System.Text.RegularExpressions;

namespace StarterKit.WebMvc.Services.Http;

/// <summary>
/// The single place that unwraps the backend's response envelope.
/// <para>
/// Wire shape: every backend controller answers through <c>ApiControllerBase.Ok()</c>, which
/// wraps the payload in a <c>Light.Contracts.Result</c> and sets the HTTP status from the
/// result code — <c>{ "requestId", "code", "isSuccess", "message", "data" }</c>, 200 on success,
/// 400/401/403/404/409/500 otherwise. Two non-envelope-by-controller failure shapes also occur:
/// the vendor exception handler writes the same envelope with a numeric <c>code</c> (e.g.
/// <c>"400"</c>) and, for a validation failure, a <c>message</c> of
/// <c>"Prop: error1,error2|Prop2: error"</c>; and a plain ASP.NET
/// <c>ValidationProblemDetails</c> (<c>{ "title", "errors": { ... } }</c>) can still surface.
/// </para>
/// <para>
/// A 2xx is only a success when its body is a success envelope — or, for calls that expect no
/// <c>data</c> (<c>expectsData</c> false), when the body is empty. An empty or
/// unreadable body on a call that expects data is a failure (<see cref="ApiResultCodes.Error"/>),
/// logged as a warning; it is never reported as success.
/// </para>
/// </summary>
internal static partial class ApiResponseReader
{
    private const string UnexpectedResponseMessage = "The server returned an unexpected response. Please try again.";

    public static async Task<ApiResult<T>> ReadAsync<T>(
        HttpResponseMessage response,
        bool throwOnAuthFailure,
        bool expectsData,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;
        var body = await ReadBodyAsync<T>(
            response,
            logger,
            cancellationToken);

        var envelope = body.Envelope;

        if (throwOnAuthFailure
            && statusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            throw new ApiAuthorizationException(
                statusCode,
                FirstNonEmpty(envelope?.Message, envelope?.Title) ?? $"Backend returned {statusCode}.");
        }

        if (response.IsSuccessStatusCode)
        {
            if (envelope is not null && IsSuccessEnvelope(envelope))
            {
                return new ApiResult<T>
                {
                    IsSuccess = true,
                    Code = ApiResultCodes.Success,
                    Message = envelope.Message ?? string.Empty,
                    StatusCode = statusCode,
                    RequestId = envelope.RequestId,
                    Data = envelope.Data,
                };
            }

            if (body.IsEmpty && !expectsData)
            {
                return new ApiResult<T>
                {
                    IsSuccess = true,
                    Code = ApiResultCodes.Success,
                    StatusCode = statusCode,
                };
            }

            if (envelope is null)
            {
                logger.LogWarning(
                    body.ParseError,
                    "Backend {Method} {Uri} answered {StatusCode} with an {BodyState} body where a result envelope was expected.",
                    response.RequestMessage?.Method,
                    response.RequestMessage?.RequestUri,
                    statusCode,
                    body.IsEmpty ? "empty" : "unreadable");

                return new ApiResult<T>
                {
                    IsSuccess = false,
                    Code = ApiResultCodes.Error,
                    Message = UnexpectedResponseMessage,
                    StatusCode = statusCode,
                };
            }
        }

        var validationErrors = ReadValidationErrors(
            envelope,
            statusCode);

        return new ApiResult<T>
        {
            IsSuccess = false,
            Code = ResolveFailureCode(
                envelope?.Code,
                statusCode),
            Message = FirstNonEmpty(envelope?.Message, envelope?.Title, validationErrors.Values.FirstOrDefault()?.FirstOrDefault())
                ?? $"Backend request failed with status {statusCode}.",
            StatusCode = statusCode,
            RequestId = envelope?.RequestId,
            ValidationErrors = validationErrors,
        };
    }

    private static async Task<ResponseBody<T>> ReadBodyAsync<T>(
        HttpResponseMessage response,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            return new ResponseBody<T>(
                null,
                true,
                null);
        }

        try
        {
            return new ResponseBody<T>(
                JsonSerializer.Deserialize<Envelope<T>>(
                    content,
                    ApiJson.Options),
                false,
                null);
        }
        catch (JsonException ex)
        {
            // Non-JSON (e.g. a proxy's HTML error page) or unexpectedly shaped body. A failure
            // status is still reported from the HTTP status alone; a 2xx becomes a failure above.
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    ex,
                    "Backend {Method} {Uri} answered {StatusCode} with a body that is not a result envelope.",
                    response.RequestMessage?.Method,
                    response.RequestMessage?.RequestUri,
                    (int)response.StatusCode);
            }

            return new ResponseBody<T>(
                null,
                false,
                ex);
        }
    }

    private static bool IsSuccessEnvelope<T>(Envelope<T> envelope)
    {
        return envelope.IsSuccess
            ?? string.Equals(envelope.Code, ApiResultCodes.Success, StringComparison.Ordinal);
    }

    /// <summary>
    /// The vendor exception handler writes the HTTP status as the code (e.g. "400") — map it back to
    /// the named vocabulary. A failure is never coded <see cref="ApiResultCodes.Success"/>, even when
    /// it arrived with a 2xx status.
    /// </summary>
    private static string ResolveFailureCode(
        string? code,
        int statusCode)
    {
        var resolved = string.IsNullOrEmpty(code) || int.TryParse(code, out _)
            ? ApiResultCodes.FromStatusCode(statusCode)
            : code;

        return resolved == ApiResultCodes.Success
            ? ApiResultCodes.Error
            : resolved;
    }

    private static IReadOnlyDictionary<string, string[]> ReadValidationErrors<T>(
        Envelope<T>? envelope,
        int statusCode)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (envelope?.Errors is { Count: > 0 })
        {
            foreach (var (key, messages) in envelope.Errors)
            {
                errors[key] = messages;
            }

            return errors;
        }

        if (statusCode != StatusCodes.Status400BadRequest || string.IsNullOrEmpty(envelope?.Message))
        {
            return errors;
        }

        // "Prop: error1,error2|Prop2: error" — the flattened ModelState/FluentValidation format.
        // Messages are kept whole (not split on ',') since a message may itself contain commas.
        foreach (var segment in envelope.Message.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = segment.IndexOf(": ", StringComparison.Ordinal);

            if (separator <= 0)
            {
                continue;
            }

            var key = segment[..separator].Trim();

            if (!PropertyPathPattern().IsMatch(key))
            {
                continue;
            }

            errors[key] = [segment[(separator + 2)..].Trim()];
        }

        return errors;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    [GeneratedRegex(@"^[A-Za-z_][\w.\[\]]*$")]
    private static partial Regex PropertyPathPattern();

    private sealed record ResponseBody<T>(
        Envelope<T>? Envelope,
        bool IsEmpty,
        JsonException? ParseError);

    private sealed class Envelope<T>
    {
        public string? RequestId { get; set; }

        public string? Code { get; set; }

        public bool? IsSuccess { get; set; }

        public string? Message { get; set; }

        public T? Data { get; set; }

        // ValidationProblemDetails members.
        public string? Title { get; set; }

        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
