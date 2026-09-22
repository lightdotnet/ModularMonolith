using StarterKit.Locations.Contracts.Locations;

namespace StarterKit.Locations.Contracts.Services;

/// <summary>
/// Cross-module, read-only seam for resolving Location data — consumed by other modules (Catalog,
/// Orders, Inventory) without exposing the Location aggregate or its EF internals outside this
/// module. Same role as Organization's <c>IOrgDirectoryService</c>.
/// </summary>
public interface ILocationDirectoryService
{
    /// <summary>Resolves a single location by id, or <c>null</c> if it does not exist.</summary>
    Task<LocationDto?> GetAsync(string locationId, CancellationToken cancellationToken = default);

    /// <summary>Whether a location with this id exists.</summary>
    Task<bool> ExistsAsync(string locationId, CancellationToken cancellationToken = default);

    /// <summary>Resolves the immediate children of a location.</summary>
    Task<IReadOnlyList<LocationLookupDto>> GetChildrenAsync(
        string locationId, CancellationToken cancellationToken = default);

    /// <summary>Resolves every location as a thin lookup list, for populating pickers.</summary>
    Task<IReadOnlyList<LocationLookupDto>> GetLookupAsync(CancellationToken cancellationToken = default);
}
