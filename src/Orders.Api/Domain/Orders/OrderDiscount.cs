using Light.Domain.ValueObjects;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Orders.Api.Domain.Orders;

/// <summary>
/// A single discount applied to an <see cref="Order"/>, either a flat <see cref="OrderDiscountKind.FixedAmount"/>
/// or a <see cref="OrderDiscountKind.Percentage"/> of the order subtotal. Mapped as an <b>optional</b>
/// EF owned type (table-split, same row) — unlike <see cref="StarterKit.Shared.ValueObjects.Money"/>/
/// <see cref="StarterKit.Shared.ValueObjects.VatPercentage"/> on <c>Product</c>, which are always
/// present (<c>Navigation(...).IsRequired()</c>), an order may carry no discount at all. Clearing it
/// back to <c>null</c> (<see cref="Order.RemoveDiscount"/>) is a plain, safe reference assignment;
/// <see cref="Update"/> exists so an already-present discount is mutated in place instead of being
/// reassigned to a new instance, which would leave the new values unpersisted — see
/// <see cref="StarterKit.Shared.ValueObjects.Money"/>'s own doc comment for the mechanism.
/// </summary>
public sealed class OrderDiscount : ValueObject
{
    // EF materialises the owned type through this parameterless constructor + the property
    // setters; stored rows are always already valid, so the guard is not re-run on read.
    private OrderDiscount()
    {
    }

    public OrderDiscount(
        OrderDiscountKind kind,
        decimal value)
    {
        Validate(kind, value);

        Kind = kind;
        Value = value;
    }

    public OrderDiscountKind Kind { get; private set; }

    public decimal Value { get; private set; }

    /// <summary>
    /// Mutates this same tracked instance in place rather than being replaced by a new one —
    /// mirrors <see cref="StarterKit.Shared.ValueObjects.Money.Update"/>.
    /// </summary>
    internal void Update(
        OrderDiscountKind kind,
        decimal value)
    {
        Validate(kind, value);

        Kind = kind;
        Value = value;
    }

    public decimal ComputeAmount(decimal subtotal) => Kind switch
    {
        OrderDiscountKind.FixedAmount => Value,
        OrderDiscountKind.Percentage => subtotal * Value / 100m,
        _ => 0m,
    };

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Kind;
        yield return Value;
    }

    private static void Validate(
        OrderDiscountKind kind,
        decimal value)
    {
        if (kind == OrderDiscountKind.Percentage && (value < 0 || value > 100))
            throw Invalid(nameof(value), "Percentage discount must be between 0 and 100.");

        if (kind == OrderDiscountKind.FixedAmount && value < 0)
            throw Invalid(nameof(value), "Fixed amount discount cannot be negative.");
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
