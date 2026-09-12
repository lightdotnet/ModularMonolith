using StarterKit.Catalog.Api.Data;
using StarterKit.Catalog.Api.Domain.Products;
using StarterKit.Shared.ValueObjects;

namespace StarterKit.Catalog.Api.Application.Products.Commands;

internal sealed record CreateProductCommand(CreateProductRequest Model) : ICommand<IResult<string>>;

internal sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Model).SetValidator(new CreateProductRequestValidator());
    }
}

internal class CreateProductCommandHandler(CatalogDbContext context)
    : ICommandHandler<CreateProductCommand, IResult<string>>
{
    public async Task<IResult<string>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var categoryExists = await context.Categories
            .AnyAsync(x => x.Id == model.CategoryId, cancellationToken);

        if (!categoryExists)
            return Result<string>.NotFound($"Category {model.CategoryId} not found");

        var sku = new Sku(model.Sku);

        // Equality filtering against a HasConversion-mapped property is a well-supported EF Core
        // pattern (the converter is applied to the client value before the SQL comparison) — unlike
        // projecting further member access (Sku.Value) inside a Select, which this repo avoids; see
        // CatalogPricingService's doc comment.
        var skuTaken = await context.Products
            .AnyAsync(x => x.Sku == sku, cancellationToken);

        if (skuTaken)
            return Result<string>.Error($"SKU '{model.Sku}' already exists.");

        var entity = Product.Create(
            model.CategoryId,
            model.Name,
            model.Description,
            sku,
            new Money(model.Price, model.Currency),
            new VatPercentage(model.VatRate));

        await context.Products.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(entity.Id);
    }
}
