using StarterKit.Currencies.Api.Data;
using StarterKit.Currencies.Api.Domain.Currencies;

namespace StarterKit.Currencies.Api.Application.Currencies.Queries;

internal sealed record GetCurrencyByCodeQuery(string Code) : IQuery<IResult<CurrencyDto>>;

internal class GetCurrencyByCodeQueryHandler(CurrencyDbContext context)
    : IQueryHandler<GetCurrencyByCodeQuery, IResult<CurrencyDto>>
{
    public async Task<IResult<CurrencyDto>> Handle(
        GetCurrencyByCodeQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Currencies
            .AsNoTracking()
            .Where(new CurrencyByCodeSpec(request.Code))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result<CurrencyDto>.NotFound($"Currency {request.Code} not found");

        return Result<CurrencyDto>.Success(ToDto(entity));
    }

    internal static CurrencyDto ToDto(Currency entity) => new()
    {
        Code = entity.Code,
        Name = entity.Name,
        Symbol = entity.Symbol,
        DecimalPlaces = entity.DecimalPlaces,
        IsActive = entity.IsActive,
        IsBase = entity.IsBase,
    };
}
