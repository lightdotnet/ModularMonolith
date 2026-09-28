using System.Net;

namespace StarterKit.WebMvc.Tests.TestSupport;

/// <summary>
/// Answers every request through the supplied delegate — no network, no real backend.
/// </summary>
public sealed class FakeHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    public static FakeHttpMessageHandler Returning(
        HttpStatusCode statusCode,
        string? body = null)
    {
        return new FakeHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    body ?? string.Empty,
                    System.Text.Encoding.UTF8,
                    "application/json"),
            };

            return Task.FromResult(response);
        });
    }

    public static FakeHttpMessageHandler Throwing(Exception exception)
    {
        return new FakeHttpMessageHandler((_, _) => Task.FromException<HttpResponseMessage>(exception));
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);

        return respond(
            request,
            cancellationToken);
    }
}
