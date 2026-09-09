using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Notifications.Contracts.SystemNotifications;

namespace StarterKit.Notifications.Api.SignalR;

/// <summary>
/// Registration and validation for <see cref="NotificationHubOptions"/>. The bound
/// <c>Notifications:Hub:Path</c> drives authentication scheme routing for every request in
/// the co-host, so an invalid value must fail fast at startup rather than silently break the
/// auth pipeline. Both the SignalR module and the WebApi host bind through here so they agree
/// on one validated value.
/// </summary>
public static class NotificationHubOptionsSetup
{
    /// <summary>
    /// Failure message shared by the options validator and the WebApi host's own inline guard
    /// (the host reads the raw section before <c>ValidateOnStart</c> runs).
    /// </summary>
    public const string InvalidPathMessage =
        "Configuration 'Notifications:Hub:Path' must be an absolute path (starting with '/', "
        + "longer than '/') that does not overlap the '/api' route namespace.";

    public static IServiceCollection AddNotificationHubOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<NotificationHubOptions>()
            .BindConfiguration(NotificationHubOptions.SectionName)
            .Validate(options => IsValidHubPath(options.Path), InvalidPathMessage)
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// A valid hub path is a non-empty absolute path, longer than <c>/</c>, that neither sits
    /// inside nor is a parent of the <c>/api</c> route namespace. The scheme selector checks
    /// the hub branch before the <c>/api</c> branch, so any overlap would shadow the whole API.
    /// </summary>
    public static bool IsValidHubPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length <= 1 || path[0] != '/')
            return false;

        var hubPath = new PathString(path);

        return !hubPath.StartsWithSegments("/api")
            && !new PathString("/api").StartsWithSegments(hubPath);
    }
}
