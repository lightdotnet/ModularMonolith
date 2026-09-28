using FluentValidation;
using StarterKit.Shared;

namespace StarterKit.Identity.Contracts;

/// <summary>
/// Query parameters for the paginated user search endpoint (search term + pagination + optional sort).
/// </summary>
public record SearchUserRequest : SearchQuery
{
    /// <summary>
    /// Optional sort field, one of <see cref="SearchUserSortFields.All"/> (case-insensitive).
    /// When absent, the default ordering applies (newest first, then user name).
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Optional sort direction, <c>asc</c> or <c>desc</c> (case-insensitive); defaults to <c>asc</c>
    /// when <see cref="SortBy"/> is given. Ignored when <see cref="SortBy"/> is absent.
    /// </summary>
    public string? SortDirection { get; set; }
}

/// <summary>
/// Whitelisted sort field names for <see cref="SearchUserRequest.SortBy"/>.
/// </summary>
public static class SearchUserSortFields
{
    public const string UserName = "userName";
    public const string FirstName = "firstName";
    public const string LastName = "lastName";
    public const string FullName = "fullName";
    public const string Email = "email";
    public const string Created = "created";
    public const string Status = "status";

    public static readonly IReadOnlyList<string> All =
    [
        UserName,
        FirstName,
        LastName,
        FullName,
        Email,
        Created,
        Status,
    ];
}

public sealed class SearchUserRequestValidator : AbstractValidator<SearchUserRequest>
{
    public SearchUserRequestValidator()
    {
        RuleFor(x => x.SortBy)
            .Must(sortBy => SearchUserSortFields.All.Contains(
                sortBy!,
                StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage($"SortBy must be one of: {string.Join(", ", SearchUserSortFields.All)}.");

        RuleFor(x => x.SortDirection)
            .Must(direction =>
                string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.SortDirection))
            .WithMessage("SortDirection must be 'asc' or 'desc'.");
    }
}
