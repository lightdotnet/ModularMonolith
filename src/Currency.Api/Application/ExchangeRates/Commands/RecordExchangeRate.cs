using Light.Exceptions;
using StarterKit.Currencies.Api.Data;
using StarterKit.Currencies.Api.Domain.Currencies;
using StarterKit.Currencies.Api.Domain.ExchangeRates;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;

namespace StarterKit.Currencies.Api.Application.ExchangeRates.Commands;

internal sealed record RecordExchangeRateCommand(
    RecordExchangeRateRequest Model,
    string RecordedBy) : ICommand<IResult<long>>;

internal sealed class RecordExchangeRateCommandValidator : AbstractValidator<RecordExchangeRateCommand>
{
    public RecordExchangeRateCommandValidator(IDateTime clock)
    {
        RuleFor(x => x.Model).SetValidator(new RecordExchangeRateRequestValidator());
        RuleFor(x => x.RecordedBy).NotEmpty();

        RuleFor(x => x.Model.EffectiveFrom)
            .Must(effectiveFrom => effectiveFrom <= clock.UtcNow.Add(CurrencyLimits.MaxFutureEffectiveFrom))
            .WithMessage("EffectiveFrom cannot be more than 1 day in the future.");
    }
}

/// <summary>
/// Appends a rate to the history. There is no update/delete counterpart: a correction is a newer rate
/// (a strictly later <c>EffectiveFrom</c>). The aggregate refuses a rate that is not later than the newest
/// existing one for the currency (equal or earlier); the unique index only catches a concurrent insert.
/// </summary>
internal class RecordExchangeRateCommandHandler(CurrencyDbContext context)
    : ICommandHandler<RecordExchangeRateCommand, IResult<long>>
{
    public async Task<IResult<long>> Handle(
        RecordExchangeRateCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var currency = await context.Currencies
            .Where(new CurrencyByCodeSpec(model.CurrencyCode))
            .FirstOrDefaultAsync(cancellationToken);

        if (currency is null)
            return Result<long>.NotFound($"Currency {model.CurrencyCode} not found");

        var latestEffectiveFrom = await context.ExchangeRates
            .AsNoTracking()
            .Where(x => x.CurrencyCode == currency.Code)
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => (DateTimeOffset?)x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        var entity = ExchangeRate.Record(
            currency,
            model.Rate,
            model.EffectiveFrom,
            request.RecordedBy,
            model.Note,
            latestEffectiveFrom);

        await context.ExchangeRates.AddAsync(entity, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // A concurrent writer recorded the same (currency, effective date) between the read and the insert.
            throw new ConflictException(ExchangeRate.NewerRateExistsMessage);
        }

        return Result<long>.Success(entity.Id);
    }
}
