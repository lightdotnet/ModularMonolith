using Light.Exceptions;
using StarterKit.Currencies.Api.Data;
using StarterKit.Currencies.Api.Domain.Currencies;
using StarterKit.Persistence.Extensions;

namespace StarterKit.Currencies.Api.Application.Currencies.Commands;

internal sealed record CreateCurrencyCommand(CreateCurrencyRequest Model) : ICommand<IResult<string>>;

internal sealed class CreateCurrencyCommandValidator : AbstractValidator<CreateCurrencyCommand>
{
    public CreateCurrencyCommandValidator()
    {
        RuleFor(x => x.Model).SetValidator(new CreateCurrencyRequestValidator());
    }
}

/// <summary>Creates a regular (never base) currency; the base currency is seeded, not created here.</summary>
internal class CreateCurrencyCommandHandler(CurrencyDbContext context)
    : ICommandHandler<CreateCurrencyCommand, IResult<string>>
{
    public async Task<IResult<string>> Handle(
        CreateCurrencyCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var code = Currency.NormalizeCode(model.Code);

        if (await context.Currencies.AnyAsync(x => x.Id == code, cancellationToken))
            return Result<string>.Error($"Currency '{code}' already exists.");

        var entity = Currency.Create(
            code,
            model.Name,
            model.Symbol,
            model.DecimalPlaces);

        await context.Currencies.AddAsync(entity, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // A concurrent request created the same code between the check and the insert.
            throw new ConflictException($"Currency '{code}' already exists.");
        }

        return Result<string>.Success(entity.Code);
    }
}
