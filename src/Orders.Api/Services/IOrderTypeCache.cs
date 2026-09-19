using StarterKit.Orders.Api.Domain.OrderTypes;

namespace StarterKit.Orders.Api.Services;

/// <summary>
/// In-process read cache over the full <see cref="OrderType"/> table — internal to this module (not
/// exposed via <c>Orders.Contracts</c>; no other module needs order-type lookups yet). Backed by
/// <see cref="Light.Extensions.Caching.ICacheService"/> and invalidated purely by being overwritten
/// on every write (<see cref="ReloadAsync"/>), not by time-based expiration — the order-type table is
/// small and changes rarely, so a full-list cache kept fresh on write is simpler than per-key expiry.
/// Mirrors <c>Location.Api</c>'s <c>ILocationTypeCache</c>. Replaces the former separate
/// <c>IFeeTypeCache</c>/<c>IPaymentTypeCache</c>.
/// </summary>
internal interface IOrderTypeCache
{
    /// <summary>Reloads the full order-type list from the database into cache.</summary>
    Task ReloadAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderType>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Looks up a row by the composite <c>(Id, Category)</c> key. Callers always pass the category
    /// they expect (e.g. <see cref="OrderTypeCategory.Fee"/> when resolving a fee type) — a lookup
    /// for the wrong category simply finds nothing, which is itself the guard against e.g. a
    /// Payment-category id being used as a fee type.
    /// </summary>
    Task<OrderType?> GetAsync(string id, OrderTypeCategory category, CancellationToken cancellationToken);
}
