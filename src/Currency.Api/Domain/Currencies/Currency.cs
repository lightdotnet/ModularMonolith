using Light.Exceptions;
using StarterKit.Shared.Entities;

namespace StarterKit.Currencies.Api.Domain.Currencies;

/// <summary>
/// A currency the system can hold amounts in. Its identity is the ISO 4217 code (normalized to upper case,
/// caller-supplied, like Location's <c>LocationType</c>). Exactly one currency is the base one: it is
/// created only through <see cref="CreateBase"/> (by the seeder), every currency created through the API
/// goes through <see cref="Create"/> and is never the base, there is no operation that flips the flag, and
/// a database filtered unique index on <see cref="IsBase"/> backs the "at most one base" rule. Changing the
/// base currency is deliberately out of scope for v1 (recorded rates and order amounts are all relative to
/// it). The base currency can never be deactivated. Required/length/range checks on incoming values are
/// FluentValidation's job, not this aggregate's.
/// </summary>
public class Currency : AuditableEntity
{
    private Currency()
    {
    }

    /// <summary>The ISO 4217 code; same value as <see cref="Id"/>.</summary>
    public string Code => Id;

    public string Name { get; private set; } = null!;

    public string? Symbol { get; private set; }

    /// <summary>Minor-unit digits amounts in this currency are rounded to.</summary>
    public int DecimalPlaces { get; private set; }

    public bool IsActive { get; private set; } = true;

    public bool IsBase { get; private set; }

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    /// <summary>Creates an active, non-base currency.</summary>
    public static Currency Create(
        string code,
        string name,
        string? symbol,
        int decimalPlaces) =>
        Build(
            code,
            name,
            symbol,
            decimalPlaces,
            isBase: false);

    /// <summary>
    /// Creates the base currency. Whether one already exists needs a database look and is the caller's
    /// responsibility (the seeder); the filtered unique index is the backstop.
    /// </summary>
    public static Currency CreateBase(
        string code,
        string name,
        string? symbol,
        int decimalPlaces) =>
        Build(
            code,
            name,
            symbol,
            decimalPlaces,
            isBase: true);

    public void Rename(string name) => Name = name.Trim();

    public void SetSymbol(string? symbol) => Symbol = NormalizeSymbol(symbol);

    /// <summary>
    /// Changes the minor-unit digits. Refused for the base currency (every order amount is rounded to it)
    /// and once any exchange rate has been recorded for this currency (its historical conversions would
    /// round differently). <paramref name="hasRecordedRates"/> is a database fact the caller supplies.
    /// Setting the current value again is a no-op. The 0..4 range is FluentValidation's job.
    /// </summary>
    public void SetDecimalPlaces(
        int decimalPlaces,
        bool hasRecordedRates)
    {
        if (decimalPlaces == DecimalPlaces)
            return;

        if (IsBase)
            throw new ConflictException("The decimal places of the base currency cannot be changed.");

        if (hasRecordedRates)
        {
            throw new ConflictException(
                $"The decimal places of '{Code}' cannot be changed because exchange rates have already been recorded for it.");
        }

        DecimalPlaces = decimalPlaces;
    }

    public void Activate() => IsActive = true;

    public void Deactivate()
    {
        if (IsBase)
            throw new ConflictException("The base currency cannot be deactivated.");

        IsActive = false;
    }

    private static Currency Build(
        string code,
        string name,
        string? symbol,
        int decimalPlaces,
        bool isBase) =>
        new()
        {
            Id = NormalizeCode(code),
            Name = name.Trim(),
            Symbol = NormalizeSymbol(symbol),
            DecimalPlaces = decimalPlaces,
            IsActive = true,
            IsBase = isBase,
        };

    private static string? NormalizeSymbol(string? symbol) =>
        string.IsNullOrWhiteSpace(symbol) ? null : symbol.Trim();
}
