using StarterKit.Shared;

namespace StarterKit.Identity.Contracts;

/// <summary>
/// Query parameters for the paginated user search endpoint (search term + pagination).
/// </summary>
public record SearchUserRequest : SearchQuery;
