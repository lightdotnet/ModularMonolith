using Light.Domain.ValueObjects;
using Light.Exceptions;

namespace StarterKit.Shared.ValueObjects;

/// <summary>
/// An amount paired with its currency, treated as one value. Construction (and
/// <see cref="Update"/>) is guarded: a negative amount, or a currency that is not shaped like an
/// ISO 4217 code (exactly three upper-case letters after trimming/upper-casing), is rejected with a
/// <see cref="ValidationException"/>. Whether a well-formed code is a known/active currency, and
/// which currency is the base one, is the Currency module's concern — this value object stays
/// rule-light and never converts between currencies.
/// </summary>
public sealed class Money : ValueObject
{
    // EF materialises the owned type through this parameterless constructor + the property
    // setters; stored rows are always already valid, so the guard is not re-run on read.
    private Money()
    {
    }

    public Money(
        decimal amount,
        string currency)
    {
        if (amount < 0)
            throw Invalid(nameof(amount), "Amount cannot be negative.");

        Amount = amount;
        Currency = NormalizeCurrency(currency);
    }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = null!;

    /// <summary>
    /// Mutates this same tracked instance in place rather than being replaced by a new one —
    /// mirrors <see cref="StarterKit.LeaveManagement.Api.Domain.LeaveRequests.DateRange.Update"/>
    /// and <see cref="StarterKit.Shared.ActiveStatus.Update"/>.
    /// </summary>
    internal void Update(
        decimal amount,
        string currency)
    {
        if (amount < 0)
            throw Invalid(nameof(amount), "Amount cannot be negative.");

        Amount = amount;
        Currency = NormalizeCurrency(currency);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    private static string NormalizeCurrency(string? currency)
    {
        var normalized = currency?.Trim().ToUpperInvariant();

        if (normalized is null
            || normalized.Length != 3
            || !normalized.All(c => c is >= 'A' and <= 'Z'))
        {
            throw Invalid(nameof(currency), "Currency must be a three-letter ISO 4217 code.");
        }

        return normalized;
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
