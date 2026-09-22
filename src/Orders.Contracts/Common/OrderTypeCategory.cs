namespace StarterKit.Orders.Contracts.Common;

/// <summary>
/// Discriminates the two catalogs merged into the single <c>OrderTypes</c> table — a fee type (e.g.
/// Shipping) or a payment type (e.g. Cash). Part of the composite <c>(Id, Category)</c> identity, not
/// just a display grouping — both catalogs seed a colliding code ("OTHER"), so <c>Id</c> alone is not
/// unique across categories.
/// </summary>
public enum OrderTypeCategory
{
    Fee = 0,

    Payment = 1,
}
