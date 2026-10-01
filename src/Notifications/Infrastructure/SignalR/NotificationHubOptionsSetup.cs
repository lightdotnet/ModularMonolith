using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StarterKit.Modules.Notifications.Infrastructure.SignalR;

/// <summary>
/// Registration and validation for <see cref="NotificationHubOptions"/>. The bound
/// <c>Notifications:Hub:Path</c> drives authentication scheme routing for every request in
/// the co-host, so an invalid value must fail fast at startup rather than silently break the
/// auth pipeline. Both the SignalR module and the WebApi host bind through here so they agree
/// on one validated value.
/// </summary>
public static class NotificationHubOptionsSetup
{
    private const string ApiPathPrefix = "/api";

    /// <summary>
    /// Failure message shared by the options validator and the WebApi host's own inline guard
    /// (the host reads the raw section before <c>ValidateOnStart</c> runs).
    /// </summary>
    public const string InvalidPathMessage =
        "Configuration value 'Notifications:Hub:Path' is invalid. It must be an absolute path starting "
        + "with '/' and longer than '/', without a trailing '/', must not contain whitespace, a query "
        + "string ('?') or a fragment ('#'), and must not overlap the '/api' route namespace.";

    public static IServiceCollection AddNotificationHubOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<NotificationHubOptions>()
            .BindConfiguration(NotificationHubOptions.SectionName)
            .Validate(
                options => IsValidHubPath(options.Path),
                InvalidPathMessage)
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// A valid hub path is a non-empty absolute path, longer than <c>/</c>, with no trailing
    /// <c>'/'</c> and no whitespace, query (<c>'?'</c>) or fragment (<c>'#'</c>) — i.e. a value
    /// <see cref="PathString"/> segment matching can use as-is — that neither sits inside nor is
    /// a parent of the <c>/api</c> route namespace. The scheme selector checks the hub branch
    /// before the <c>/api</c> branch, so any overlap would shadow the whole API.
    /// </summary>
    public static bool IsValidHubPath(string? path)
    {
        if (string.IsNullOrEmpty(path)
            || path.Length <= 1
            || path[0] != '/'
            || path[^1] == '/'
            || path.Any(char.IsWhiteSpace)
            || path.IndexOfAny(['?', '#']) >= 0)
        {
            return false;
        }

        var hubPath = new PathString(path);
        var apiPath = new PathString(ApiPathPrefix);

        return !hubPath.StartsWithSegments(apiPath)
            && !apiPath.StartsWithSegments(hubPath);
    }
}
