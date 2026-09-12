using StarterKit.Catalog.Api.Data;
using StarterKit.Catalog.Api.Domain.Categories;

namespace StarterKit.Catalog.Api.Application.Categories.Commands;

internal sealed record DeleteCategoryCommand(string Id) : ICommand<IResult>;

internal sealed class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

internal class DeleteCategoryCommandHandler(CatalogDbContext context)
    : ICommandHandler<DeleteCategoryCommand, IResult>
{
    public async Task<IResult> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Categories
            .Where(new CategoryByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Category {request.Id} not found");

        var hasChildren = await context.Categories
            .AnyAsync(x => x.ParentCategoryId == request.Id, cancellationToken);

        if (hasChildren)
            return Result.Error("Category still has child categories. Remove them first.");

        var hasProducts = await context.Products
            .AnyAsync(x => x.CategoryId == request.Id, cancellationToken);

        if (hasProducts)
            return Result.Error("Category still has products assigned. Reassign or remove them first.");

        context.Categories.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
