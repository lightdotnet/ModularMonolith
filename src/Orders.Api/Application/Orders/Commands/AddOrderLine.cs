using StarterKit.Catalog.Contracts.Common;
using Light.Exceptions;
using ValidationException = Light.Exceptions.ValidationException;
using StarterKit.Catalog.Contracts.Services;
using StarterKit.Currencies.Contracts.Services;
using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Shared;
using StarterKit.Shared.ValueObjects;

namespace StarterKit.Orders.Api.Application.Orders.Commands;

internal sealed record AddOrderLineCommand(
    long OrderId,
    AddOrderLineRequest Model) : ICommand<IResult>;

internal sealed class AddOrderLineCommandValidator : AbstractValidator<AddOrderLineCommand>
{
    public AddOrderLineCommandValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new AddOrderLineRequestValidator());
    }
}

internal class AddOrderLineCommandHandler(
    OrdersDbContext context,
    ICatalogPricingService catalogPricingService,
    ICurrencyService currencyService,
    IDateTime clock)
    : ICommandHandler<AddOrderLineCommand, IResult>
{
    // Matches the decimal(18,2) Money columns of the Orders schema.
    private const int MaxSupportedDecimalPlaces = 2;

    private const decimal MaxColumnAmount = 9_999_999_999_999_999.99m;

    public async Task<IResult> Handle(
        AddOrderLineCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Orders
            .Include(x => x.Lines)
            .Where(new OrderByIdSpec(request.OrderId))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Order {request.OrderId} not found");

        var model = request.Model;

        var priceInfo = await catalogPricingService.GetPriceInfoAsync(model.ProductId, cancellationToken);

        if (priceInfo is null || priceInfo.Status != ProductStatus.Active)
            return Result.NotFound($"Product {model.ProductId} not found or is not active");

        var catalogPrice = new Money(priceInfo.Price, priceInfo.Currency);
        var unitPrice = catalogPrice;
        CatalogPriceSnapshot? snapshot = null;

        // Orders are computed and stored in one currency: the base currency at creation. Its decimal
        // places are also what the converted price is rounded to.
        var baseCurrency = await currencyService.GetBaseCurrencyAsync(cancellationToken);

        if (baseCurrency.Code != entity.CurrencyCode)
            throw new ConflictException(
                $"Order currency {entity.CurrencyCode} is not the base currency {baseCurrency.Code}; the order cannot take new priced lines.");

        // Order money columns are decimal(18,2): a currency with more places would be silently truncated on save.
        if (baseCurrency.DecimalPlaces > MaxSupportedDecimalPlaces)
            throw new ConflictException(
                $"The base currency uses {baseCurrency.DecimalPlaces} decimal places; order amounts support at most {MaxSupportedDecimalPlaces}.");

        // A product priced in another currency is converted once, here, at the rate effective now —
        // never re-read afterwards (only this handler and SetOrderLineSalePrice touch a line's price).
        // A missing rate throws ExchangeRateNotFoundException, surfaced as a 4xx; nothing has been
        // added to the order yet.
        if (catalogPrice.Currency != entity.CurrencyCode)
        {
            EnsureFitsColumn(catalogPrice.Amount, "Catalog price");

            var quote = await currencyService.GetRateToBaseAsync(
                catalogPrice.Currency,
                clock.UtcNow,
                cancellationToken);

            // The base currency could change between the two calls; never mix rates across bases.
            if (quote.BaseCurrencyCode.Trim().ToUpperInvariant() != entity.CurrencyCode)
                throw new ConflictException(
                    $"The exchange rate is quoted against {quote.BaseCurrencyCode}, not the order currency {entity.CurrencyCode}.");

            decimal converted;

            try
            {
                converted = CurrencyRounding.RoundToMinorUnits(catalogPrice.Amount * quote.Rate, baseCurrency.DecimalPlaces);
            }
            catch (OverflowException)
            {
                throw Invalid("The converted price is too large to store.");
            }

            EnsureFitsColumn(converted, "The converted price");

            // A non-zero catalog price must never silently become a free line.
            if (catalogPrice.Amount > 0 && converted == 0)
                throw Invalid("The converted price rounds to zero in the base currency.");

            unitPrice = new Money(converted, entity.CurrencyCode);

            snapshot = new CatalogPriceSnapshot(
                catalogPrice,
                quote.Rate,
                quote.EffectiveFrom);
        }

        var vatRate = new VatPercentage(priceInfo.VatRate);

        // A manual sale price is always entered in the order currency.
        var requestedSalePrice = model.RequestedSalePrice.HasValue
            ? new Money(model.RequestedSalePrice.Value, entity.CurrencyCode)
            : null;

        entity.AddLine(
            model.ProductId,
            priceInfo.ProductName,
            // Product.Sku is nullable since it can be cleared (Product.UpdateSku(null), through
            // UpsertProductCommand); an order line still needs a plain string snapshot, so an
            // already-cleared SKU snapshots as empty rather than null — not spelled out by the
            // retype plan, smallest reasonable call.
            priceInfo.Sku ?? string.Empty,
            model.Quantity,
            unitPrice,
            vatRate,
            requestedSalePrice,
            snapshot);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static void EnsureFitsColumn(
        decimal amount,
        string what)
    {
        if (amount > MaxColumnAmount)
            throw Invalid($"{what} is too large to store.");
    }

    private static ValidationException Invalid(string message) =>
        new(new Dictionary<string, string[]> { ["price"] = [message] });
}
