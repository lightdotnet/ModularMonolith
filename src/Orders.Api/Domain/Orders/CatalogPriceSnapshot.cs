using StarterKit.Shared.ValueObjects;

namespace StarterKit.Orders.Api.Domain.Orders;

/// <summary>
/// What a line's price looked like in Catalog before it was converted into the order currency: the
/// catalog unit price (in the catalog's own currency), the rate that was applied and when that rate
/// became effective. Only supplied when the catalog currency differs from the order currency — see
/// <see cref="Order.AddLine"/>. A plain carrier, not persisted as its own type: <see cref="OrderLine"/>
/// stores its parts as scalar snapshot columns.
/// </summary>
public sealed record CatalogPriceSnapshot(
    Money CatalogUnitPrice,
    decimal AppliedRate,
    DateTimeOffset? RateEffectiveFrom);
