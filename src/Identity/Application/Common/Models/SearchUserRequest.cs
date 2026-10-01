using StarterKit.Shared;

namespace StarterKit.Modules.Identity.Application.Common.Models;

/// <summary>
/// Query parameters for the paginated user search endpoint (search term + pagination).
/// </summary>
public record SearchUserRequest : SearchQuery;
