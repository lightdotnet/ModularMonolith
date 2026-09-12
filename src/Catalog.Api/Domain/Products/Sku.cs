using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Catalog.Api.Domain.Products;

/// <summary>
/// A stock-keeping-unit code, globally unique across every product (not scoped per category —
/// industry-standard, and <c>OrderLine</c> will snapshot the <see cref="Value"/> alone with no
/// category reference). This guard is format-only (non-blank, trimmed, capped length); uniqueness
/// itself is enforced by a handler pre-check plus a DB unique index (see
/// <c>CatalogDbContext</c>/<c>CreateProductCommandHandler</c>), not here — same belt-and-suspenders
/// split as every other uniqueness rule in this repo.
/// <para>
/// Mapped as a converted scalar column (<c>HasConversion</c>), not an owned type — EF never
/// materialises this type through reflection, only through the conversion delegate, so unlike
/// <c>Money</c>/<c>VatPercentage</c> it needs neither a private parameterless constructor nor the
/// <c>Light.Domain.ValueObjects.ValueObject</c> base (that base exists to survive the
/// tracked-owned-type <c>Deleted</c>/<c>Added</c> replace hazard described on
/// <see cref="StarterKit.LeaveManagement.Api.Domain.LeaveRequests.DateRange"/>, which only applies
/// to owned navigations, not a converted scalar).
/// </para>
/// </summary>
public sealed class Sku
{
    public const int MaxLength = 100;

    public Sku(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Invalid(nameof(value), "SKU cannot be blank.");

        value = value.Trim();

        if (value.Length > MaxLength)
            throw Invalid(nameof(value), $"SKU cannot exceed {MaxLength} characters.");

        Value = value;
    }

    public string Value { get; }

    public override bool Equals(object? obj) =>
        obj is Sku other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
