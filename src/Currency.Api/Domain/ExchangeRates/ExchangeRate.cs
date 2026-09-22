using Light.Exceptions;
using StarterKit.Currencies.Api.Domain.Currencies;
using StarterKit.Shared.Entities;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Currencies.Api.Domain.ExchangeRates;

/// <summary>
/// One recorded rate: "1 unit of <see cref="CurrencyCode"/> = <see cref="Rate"/> units of the base
/// currency", effective from <see cref="EffectiveFrom"/> until a newer rate takes over. The history is
/// append-only — there is no operation to change or remove a recorded rate, a correction is simply a newer
/// rate (and <c>CurrencyDbContext</c> refuses to save a modified/deleted rate as a backstop). The rate
/// must be positive, a rate for the base currency is meaningless and rejected, and so is one for an
/// inactive currency. A rate must also be strictly later than the newest existing one for its currency
/// (the caller supplies that from the database), which subsumes uniqueness per (currency, effective date);
/// a unique index remains as the concurrency backstop. Bounds/scale checks on incoming values are
/// FluentValidation's job.
/// </summary>
public class ExchangeRate : AuditableEntity<long>
{
    private ExchangeRate()
    {
    }

    public const string NewerRateExistsMessage =
        "A newer rate already exists; record a correction as a rate with a later effective date.";

    public string CurrencyCode { get; private set; } = null!;

    public decimal Rate { get; private set; }

    public DateTimeOffset EffectiveFrom { get; private set; }

    /// <summary>Id of the user who entered the rate.</summary>
    public string RecordedBy { get; private set; } = null!;

    public string? Note { get; private set; }

    public static ExchangeRate Record(
        Currency currency,
        decimal rate,
        DateTimeOffset effectiveFrom,
        string recordedBy,
        string? note,
        DateTimeOffset? latestEffectiveFrom)
    {
        if (currency.IsBase)
            throw Invalid(nameof(currency), "An exchange rate cannot be recorded for the base currency.");

        if (!currency.IsActive)
            throw new ConflictException($"Currency '{currency.Code}' is inactive; activate it before recording a rate.");

        if (rate <= 0)
            throw Invalid(nameof(rate), "The exchange rate must be greater than zero.");

        // No backdating: the history only grows forward, so a rate must be strictly later than the newest one.
        if (latestEffectiveFrom.HasValue && effectiveFrom <= latestEffectiveFrom.Value)
            throw new ConflictException(NewerRateExistsMessage);

        return new ExchangeRate
        {
            CurrencyCode = currency.Code,
            Rate = rate,
            EffectiveFrom = effectiveFrom,
            RecordedBy = recordedBy,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        };
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
