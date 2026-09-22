using StarterKit.Currencies.Api.Data;
using StarterKit.Currencies.Api.Domain.Currencies;

namespace StarterKit.Currencies.Api.Application.Currencies.Commands;

/// <summary>Activates or deactivates a currency; the base currency can never be deactivated (the aggregate refuses).</summary>
internal sealed record ChangeCurrencyStatusCommand(
    string Code,
    bool Activate) : ICommand<IResult>;

internal sealed class ChangeCurrencyStatusCommandValidator : AbstractValidator<ChangeCurrencyStatusCommand>
{
    public ChangeCurrencyStatusCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(CurrencyLimits.CodeLength);
    }
}

internal class ChangeCurrencyStatusCommandHandler(CurrencyDbContext context)
    : ICommandHandler<ChangeCurrencyStatusCommand, IResult>
{
    public async Task<IResult> Handle(
        ChangeCurrencyStatusCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Currencies
            .Where(new CurrencyByCodeSpec(request.Code))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Currency {request.Code} not found");

        if (request.Activate)
            entity.Activate();
        else
            entity.Deactivate();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
