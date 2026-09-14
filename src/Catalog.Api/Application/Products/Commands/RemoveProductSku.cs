using StarterKit.Catalog.Api.Data;
using StarterKit.Catalog.Api.Domain.Products;

namespace StarterKit.Catalog.Api.Application.Products.Commands;

internal sealed record RemoveProductSkuCommand(long Id) : ICommand<IResult>;

internal sealed class RemoveProductSkuCommandValidator : AbstractValidator<RemoveProductSkuCommand>
{
    public RemoveProductSkuCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal class RemoveProductSkuCommandHandler(CatalogDbContext context)
    : ICommandHandler<RemoveProductSkuCommand, IResult>
{
    public async Task<IResult> Handle(
        RemoveProductSkuCommand request,
        CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters: clearing the SKU of an already-soft-deleted product must still work
        // (that is the whole point of this endpoint — freeing the SKU for reuse) even though normal
        // reads hide soft-deleted products.
        var entity = await context.Products
            .IgnoreQueryFilters()
            .Where(new ProductByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Product {request.Id} not found");

        entity.ClearSku();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
