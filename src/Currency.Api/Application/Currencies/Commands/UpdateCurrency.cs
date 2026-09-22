using StarterKit.Currencies.Api.Data;
using StarterKit.Currencies.Api.Domain.Currencies;

namespace StarterKit.Currencies.Api.Application.Currencies.Commands;

internal sealed record UpdateCurrencyCommand(
    string Code,
    UpdateCurrencyRequest Model) : ICommand<IResult>;

internal sealed class UpdateCurrencyCommandValidator : AbstractValidator<UpdateCurrencyCommand>
{
    public UpdateCurrencyCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(CurrencyLimits.CodeLength);
        RuleFor(x => x.Model).SetValidator(new UpdateCurrencyRequestValidator());
    }
}

internal class UpdateCurrencyCommandHandler(CurrencyDbContext context)
    : ICommandHandler<UpdateCurrencyCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateCurrencyCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Currencies
            .Where(new CurrencyByCodeSpec(request.Code))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Currency {request.Code} not found");

        // Whether rates exist is a database fact the aggregate cannot see; only look when it matters.
        var hasRecordedRates = request.Model.DecimalPlaces != entity.DecimalPlaces
            && await context.ExchangeRates.AnyAsync(x => x.CurrencyCode == entity.Code, cancellationToken);

        entity.SetDecimalPlaces(request.Model.DecimalPlaces, hasRecordedRates);
        entity.Rename(request.Model.Name);
        entity.SetSymbol(request.Model.Symbol);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
