using StarterKit.Locations.Api.Domain.LocationTypes;

namespace StarterKit.Locations.Api.Services;

/// <summary>
/// In-process read cache over the full <see cref="LocationType"/> table — internal to this module
/// (not exposed via <c>Location.Contracts</c>; no other module needs location-type lookups yet).
/// Backed by <see cref="Light.Extensions.Caching.ICacheService"/> and invalidated purely by being
/// overwritten on every write (<see cref="ReloadAsync"/>), not by time-based expiration — the
/// location-type table is small and changes rarely, so a full-list cache kept fresh on write is
/// simpler than per-key expiry.
/// </summary>
internal interface ILocationTypeCache
{
    /// <summary>Reloads the full location-type list from the database into cache.</summary>
    Task ReloadAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<LocationType>> GetAllAsync(CancellationToken cancellationToken);

    Task<LocationType?> GetAsync(string id, CancellationToken cancellationToken);
}
