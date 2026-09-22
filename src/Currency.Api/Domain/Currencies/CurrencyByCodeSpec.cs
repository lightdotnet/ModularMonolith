namespace StarterKit.Currencies.Api.Domain.Currencies;

public class CurrencyByCodeSpec : Specification<Currency>
{
    public CurrencyByCodeSpec(string code)
    {
        var normalized = Currency.NormalizeCode(code);

        Where(x => x.Id == normalized);
    }
}
