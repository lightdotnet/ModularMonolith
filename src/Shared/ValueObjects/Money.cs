using Light.Domain.ValueObjects;
using Light.Exceptions;
using StarterKit.Shared.Constants;

namespace StarterKit.Shared.ValueObjects;

/// <summary>
/// An amount paired with its currency, treated as one value. Construction (and
/// <see cref="Update"/>) is guarded: a negative amount or a currency other than
/// <see cref="CurrencyConstants.Default"/> is rejected with a <see cref="ValidationException"/> —
/// the single-currency guard is deliberate for v1 and is expected to be relaxed once multi-currency
/// support is needed.
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

        if (currency != CurrencyConstants.Default)
            throw Invalid(nameof(currency), $"Currency must be '{CurrencyConstants.Default}'.");

        Amount = amount;
        Currency = currency;
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

        if (currency != CurrencyConstants.Default)
            throw Invalid(nameof(currency), $"Currency must be '{CurrencyConstants.Default}'.");

        Amount = amount;
        Currency = currency;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
