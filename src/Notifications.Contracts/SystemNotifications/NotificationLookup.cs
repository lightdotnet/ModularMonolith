using FluentValidation;
using StarterKit.Shared;

namespace StarterKit.Notifications.Contracts.SystemNotifications;

public record NotificationLookup : PageQuery
{
    public string? ToUserId { get; set; }

    public NotificationStatus? Status { get; set; }

    /// <summary>
    /// Optional sort field, one of <see cref="NotificationSortFields.All"/> (case-insensitive).
    /// When absent, the default ordering applies (newest first).
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Optional sort direction, <c>asc</c> or <c>desc</c> (case-insensitive); defaults to <c>asc</c>
    /// when <see cref="SortBy"/> is given. Ignored when <see cref="SortBy"/> is absent.
    /// </summary>
    public string? SortDirection { get; set; }
}

/// <summary>
/// Whitelisted sort field names for <see cref="NotificationLookup.SortBy"/>.
/// </summary>
public static class NotificationSortFields
{
    public const string Created = "created";
    public const string Title = "title";
    public const string Status = "status";
    public const string FromName = "fromName";

    public static readonly IReadOnlyList<string> All =
    [
        Created,
        Title,
        Status,
        FromName,
    ];
}

public sealed class NotificationLookupValidator : AbstractValidator<NotificationLookup>
{
    public NotificationLookupValidator()
    {
        RuleFor(x => x.SortBy)
            .Must(sortBy => NotificationSortFields.All.Contains(
                sortBy!,
                StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage($"SortBy must be one of: {string.Join(", ", NotificationSortFields.All)}.");

        RuleFor(x => x.SortDirection)
            .Must(direction =>
                string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.SortDirection))
            .WithMessage("SortDirection must be 'asc' or 'desc'.");
    }
}
