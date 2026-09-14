using StarterKit.Catalog.Api.Data;
using StarterKit.Catalog.Api.Domain.Products;

namespace StarterKit.Catalog.Api.Application.Products.Commands;

internal sealed record DeleteProductCommand(long Id) : ICommand<IResult>;

internal sealed class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal class DeleteProductCommandHandler(CatalogDbContext context)
    : ICommandHandler<DeleteProductCommand, IResult>
{
    public async Task<IResult> Handle(
        DeleteProductCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Products
            .Where(new ProductByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Product {request.Id} not found");

        entity.Delete();

        // The actual soft-delete is TrackingExtensions.AuditEntries intercepting this Remove() call
        // (enableSoftDelete: true on CatalogDbContext) — it stamps Deleted/DeletedBy and flips the
        // entry back to Modified instead of letting EF issue a real DELETE.
        context.Products.Remove(entity);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
