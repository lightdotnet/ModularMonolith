using StarterKit.Catalog.Api.Data;
using StarterKit.Catalog.Api.Domain.Products;
using StarterKit.Shared.ValueObjects;

namespace StarterKit.Catalog.Api.Application.Products.Commands;

/// <summary>
/// One general-purpose update covering name/description/price/VAT/category together, even though
/// the domain stays fine-grained internally (<c>Rename</c>/<c>Reprice</c>/<c>UpdateVatRate</c>/
/// <c>UpdateDescription</c>/<c>Recategorize</c> remain separate guarded methods, called together
/// here). <c>Activate</c>/<c>Deactivate</c> get their own commands/endpoints — status transitions,
/// not field edits — mirrors why Location/OrgUnit split <c>Move</c> out but keep other edits as one
/// <c>PUT</c>.
/// </summary>
internal sealed record UpdateProductCommand(long Id, UpdateProductRequest Model) : ICommand<IResult>;

internal sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new UpdateProductRequestValidator());
    }
}

internal class UpdateProductCommandHandler(CatalogDbContext context)
    : ICommandHandler<UpdateProductCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var entity = await context.Products
            .Where(new ProductByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Product {request.Id} not found");

        var categoryExists = await context.Categories
            .AnyAsync(x => x.Id == model.CategoryId, cancellationToken);

        if (!categoryExists)
            return Result.NotFound($"Category {model.CategoryId} not found");

        entity.Rename(model.Name);
        entity.UpdateDescription(model.Description);
        entity.Reprice(new Money(model.Price, model.Currency));
        entity.UpdateVatRate(new VatPercentage(model.VatRate));
        entity.Recategorize(model.CategoryId);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
