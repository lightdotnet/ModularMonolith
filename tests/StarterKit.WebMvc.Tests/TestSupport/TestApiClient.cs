using Microsoft.Extensions.Logging.Abstractions;
using StarterKit.WebMvc.Services.Http;

namespace StarterKit.WebMvc.Tests.TestSupport;

public sealed record SampleDto(
    int Id,
    string Name);

/// <summary>
/// Minimal concrete <see cref="ApiClientBase"/> so the send + envelope-unwrapping path can be
/// exercised end to end against a <see cref="FakeHttpMessageHandler"/>.
/// </summary>
internal sealed class TestApiClient(
    HttpClient httpClient)
    : ApiClientBase(
        httpClient,
        NullLogger.Instance)
{
    public static TestApiClient Create(
        HttpMessageHandler handler,
        TimeSpan? timeout = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://backend.test/"),
        };

        if (timeout is not null)
        {
            httpClient.Timeout = timeout.Value;
        }

        return new TestApiClient(httpClient);
    }

    public Task<ApiResult<SampleDto>> GetSampleAsync(
        ApiRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<SampleDto>(
            HttpMethod.Get,
            "sample",
            request,
            cancellationToken);
    }

    public Task<ApiResult> DeleteSampleAsync(
        ApiRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            HttpMethod.Delete,
            "sample",
            request,
            cancellationToken);
    }
}
