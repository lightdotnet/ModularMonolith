using System.Diagnostics;
using Light.Mediator;
using Microsoft.Extensions.Logging;

namespace StarterKit.Shared;

/// <summary>
/// Logs the name and elapsed time of every mediator request. Deliberately does not
/// log request or response bodies - they can carry secrets (e.g. passwords, tokens).
/// </summary>
public class LoggingBehaviour<TRequest, TResponse>(
    ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling {RequestName}", requestName);

        var startTimestamp = Stopwatch.GetTimestamp();
        var response = await next(cancellationToken);
        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

        logger.LogInformation(
            "Handled {RequestName} in {ElapsedMs} ms", requestName, elapsed.TotalMilliseconds);

        return response;
    }
}
