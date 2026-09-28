using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace StarterKit.WebMvc.Services.Http;

/// <summary>
/// Per-call options for <see cref="ApiClientBase"/>.
/// </summary>
internal sealed record ApiRequest
{
    /// <summary>JSON request body; <c>null</c> sends no body.</summary>
    public object? Body { get; init; }

    /// <summary>Query-string values; <c>null</c> values are omitted.</summary>
    public IReadOnlyDictionary<string, string?>? Query { get; init; }

    /// <summary>Skips <see cref="BearerTokenHandler"/> — for the anonymous auth endpoints.</summary>
    public bool Anonymous { get; init; }

    /// <summary>
    /// Bearer token to send instead of the one in the current cookie session — for calls made
    /// while the session is being established/refreshed and the cookie does not carry it yet.
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// When true (the default), a 401/403 response is raised as
    /// <see cref="ApiAuthorizationException"/> instead of being returned as a result.
    /// </summary>
    public bool ThrowOnAuthFailure { get; init; } = true;
}

/// <summary>
/// Base for the typed module clients: builds the request, sends it through the module's named
/// <see cref="HttpClient"/> and unwraps the envelope via <see cref="ApiResponseReader"/>.
/// Transport failures (network, timeout) come back as a failed <see cref="ApiResult"/> rather
/// than an exception, mirroring the admin client's <c>guardCall</c>.
/// </summary>
internal abstract class ApiClientBase(
    HttpClient httpClient,
    ILogger logger)
{
    protected Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        ApiRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        return SendCoreAsync<T>(
            method,
            path,
            request,
            expectsData: true,
            cancellationToken);
    }

    /// <summary>For endpoints whose success response carries no <c>data</c> (a 2xx with an empty body is a success).</summary>
    protected async Task<ApiResult> SendAsync(
        HttpMethod method,
        string path,
        ApiRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        return await SendCoreAsync<object>(
            method,
            path,
            request,
            expectsData: false,
            cancellationToken);
    }

    /// <summary>Escapes a caller-supplied value for use as one route segment.</summary>
    protected static string Segment(string value) => Uri.EscapeDataString(value);

    private async Task<ApiResult<T>> SendCoreAsync<T>(
        HttpMethod method,
        string path,
        ApiRequest? request,
        bool expectsData,
        CancellationToken cancellationToken)
    {
        request ??= new ApiRequest();

        using var message = BuildMessage(
            method,
            path,
            request);

        HttpResponseMessage response;

        try
        {
            response = await httpClient.SendAsync(
                message,
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(
                ex,
                "Backend request {Method} {Path} failed before a response was received.",
                method,
                path);

            return ApiResult.TransportFailure<T>("The server could not be reached. Please try again.");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                ex,
                "Backend request {Method} {Path} timed out.",
                method,
                path);

            return ApiResult.TransportFailure<T>("The server did not respond in time. Please try again.");
        }

        using (response)
        {
            return await ApiResponseReader.ReadAsync<T>(
                response,
                request.ThrowOnAuthFailure,
                expectsData,
                logger,
                cancellationToken);
        }
    }

    private static HttpRequestMessage BuildMessage(
        HttpMethod method,
        string path,
        ApiRequest request)
    {
        var uri = request.Query is null
            ? path
            : QueryHelpers.AddQueryString(
                path,
                request.Query.Where(pair => pair.Value is not null));

        var message = new HttpRequestMessage(
            method,
            uri);

        if (request.Body is not null)
        {
            message.Content = JsonContent.Create(
                request.Body,
                request.Body.GetType(),
                options: ApiJson.Options);
        }

        if (request.Anonymous)
        {
            message.Options.Set(
                BearerTokenHandler.AnonymousOption,
                true);
        }

        if (!string.IsNullOrEmpty(request.AccessToken))
        {
            message.Options.Set(
                BearerTokenHandler.AccessTokenOption,
                request.AccessToken);
        }

        return message;
    }
}
