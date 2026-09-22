using StarterKit.Catalog.Api.Data;
using StarterKit.Catalog.Api.Domain.Products;
using StarterKit.Currencies.Contracts.Services;
using StarterKit.Shared.ValueObjects;

namespace StarterKit.Catalog.Api.Application.Products.Commands;

/// <summary>
/// Replaces the separate <c>CreateProductCommand</c>/<c>UpdateProductCommand</c> with one upsert —
/// <c>Id is null</c> creates, <c>Id is { }</c> updates — so a single request shape carries images on
/// both paths (previously create-only omitted them) and the update path can no longer forget to load
/// <see cref="Product.Images"/> before replacing them. The domain stays fine-grained internally
/// (<see cref="Product.Rename"/>/<see cref="Product.Reprice"/>/<see cref="Product.UpdateVatRate"/>/
/// <see cref="Product.UpdateDescription"/>/<see cref="Product.Recategorize"/> remain separate guarded
/// methods, called together here). <c>Activate</c>/<c>Deactivate</c> keep their own commands/endpoints
/// — status transitions, not field edits — mirrors why Location/OrgUnit split <c>Move</c> out but keep
/// other edits as one <c>PUT</c>.
/// </summary>
internal sealed record UpsertProductCommand(long? Id, UpsertProductRequest Model) : ICommand<IResult<long>>;

internal sealed class UpsertProductCommandValidator : AbstractValidator<UpsertProductCommand>
{
    public UpsertProductCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).When(x => x.Id is not null);
        RuleFor(x => x.Model).SetValidator(new UpsertProductRequestValidator());
        RuleFor(x => x.Model.Sku).NotEmpty().When(x => x.Id is null);
    }
}

internal class UpsertProductCommandHandler(
    CatalogDbContext context,
    ICurrencyService currencyService)
    : ICommandHandler<UpsertProductCommand, IResult<long>>
{
    public async Task<IResult<long>> Handle(
        UpsertProductCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var categoryExists = await context.Categories
            .AnyAsync(x => x.Id == model.CategoryId, cancellationToken);

        if (!categoryExists)
            return Result<long>.NotFound($"Category {model.CategoryId} not found");

        Product entity;

        if (request.Id is { } id)
        {
            var existing = await context.Products
                .Where(new ProductByIdSpec(id))
                .Include(x => x.Images)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is null)
                return Result<long>.NotFound($"Product {id} not found");

            // A product may be priced in any active currency the Currency module knows. An unchanged
            // currency is not re-checked, so a product whose currency was deactivated later can still
            // be edited; only a currency change has to land on an active one.
            var currencyUnchanged = string.Equals(
                existing.Price.Currency,
                model.Currency.Trim(),
                StringComparison.OrdinalIgnoreCase);

            if (!currencyUnchanged && !await IsActiveCurrencyAsync(model.Currency, cancellationToken))
                return Result<long>.NotFound($"Currency {model.Currency} not found or is not active");

            entity = existing;

            if (!string.IsNullOrEmpty(model.Sku))
            {
                // IgnoreQueryFilters: a soft-deleted product's SKU is filtered out of the default query, so
                // without this the pre-check would report the SKU as "available" right before SaveChanges
                // throws a raw DB unique-constraint exception instead of this friendly error — the SKU
                // unique index is deliberately not scoped by Deleted (see CatalogDbContext).
                // Equality filtering against a HasConversion-mapped property is a well-supported EF Core
                // pattern (the converter is applied to the client value before the SQL comparison) — unlike
                // projecting further member access (Sku.Value) inside a Select, which this repo avoids; see
                // CatalogPricingService's doc comment.
                var skuTaken = await context.Products
                    .IgnoreQueryFilters()
                    .AnyAsync(x =>
                        x.Id != id
                        && x.Sku != null
                        && x.Sku == new Sku(model.Sku),
                        cancellationToken);

                if (skuTaken)
                    return Result<long>.Error($"SKU '{model.Sku}' already exists.");
            }

            entity.Rename(model.Name);
            entity.UpdateDescription(model.Description);
            entity.Reprice(new Money(model.Price, model.Currency));
            entity.UpdateVatRate(new VatPercentage(model.VatRate));
            entity.Recategorize(model.CategoryId);
            entity.UpdateSku(model.Sku);

            entity.RemoveImages();
            foreach (var image in model.Images)
            {
                entity.AddImage(new ProductImageUrl(
                    image.Url,
                    image.SortOrder));
            }
        }
        else
        {
            if (!await IsActiveCurrencyAsync(model.Currency, cancellationToken))
                return Result<long>.NotFound($"Currency {model.Currency} not found or is not active");

            var sku = new Sku(model.Sku!);

            // IgnoreQueryFilters: a soft-deleted product's SKU is filtered out of the default query, so
            // without this the pre-check would report the SKU as "available" right before SaveChanges
            // throws a raw DB unique-constraint exception instead of this friendly error — the SKU
            // unique index is deliberately not scoped by Deleted (see CatalogDbContext).
            // Equality filtering against a HasConversion-mapped property is a well-supported EF Core
            // pattern (the converter is applied to the client value before the SQL comparison) — unlike
            // projecting further member access (Sku.Value) inside a Select, which this repo avoids; see
            // CatalogPricingService's doc comment.
            var skuTaken = await context.Products
                .IgnoreQueryFilters()
                .AnyAsync(x => x.Sku != null && x.Sku == sku, cancellationToken);

            if (skuTaken)
                return Result<long>.Error($"SKU '{model.Sku}' already exists.");

            entity = Product.Create(
                model.CategoryId,
                model.Name,
                model.Description,
                sku,
                new Money(model.Price, model.Currency),
                new VatPercentage(model.VatRate));

            foreach (var image in model.Images)
            {
                entity.AddImage(new ProductImageUrl(
                    image.Url,
                    image.SortOrder));
            }

            await context.Products.AddAsync(entity, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result<long>.Success(entity.Id);
    }

    private async Task<bool> IsActiveCurrencyAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var currency = await currencyService.GetAsync(code, cancellationToken);

        return currency is { IsActive: true };
    }
}
