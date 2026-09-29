using Light.Domain.ValueObjects;
using Light.Exceptions;

namespace StarterKit.Shared.ValueObjects;

/// <summary>
/// A VAT rate expressed as a percentage in the inclusive range [0, 100]. Construction (and
/// <see cref="Update"/>) is guarded: a value outside that range is rejected with a
/// <see cref="ValidationException"/>.
/// </summary>
public sealed class VatPercentage : ValueObject
{
    // EF materialises the owned type through this parameterless constructor + the property
    // setter; stored rows are always already valid, so the guard is not re-run on read.
    private VatPercentage()
    {
    }

    public VatPercentage(decimal value)
    {
        if (value < 0 || value > 100)
            throw Invalid(nameof(value), "VAT percentage must be between 0 and 100.");

        Value = value;
    }

    public decimal Value { get; private set; }

    /// <summary>
    /// Mutates this same tracked instance in place rather than being replaced by a new one —
    /// mirrors <see cref="StarterKit.LeaveManagement.Api.Domain.LeaveRequests.DateRange.Update"/>
    /// and <see cref="StarterKit.Shared.ActiveStatus.Update"/>.
    /// </summary>
    internal void Update(decimal value)
    {
        if (value < 0 || value > 100)
            throw Invalid(nameof(value), "VAT percentage must be between 0 and 100.");

        Value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
