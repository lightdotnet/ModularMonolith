namespace Monolith.Catalog.UseCases.Categories.Commands;

public record DeleteCategoryCommand(string Id) : ICommand<Result>;

internal class DeleteCategoryCommandHandler(CatalogContext context) :
    ICommandHandler<DeleteCategoryCommand, Result>
{
    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await context.Set<Category>()
            .Where(new CategoryByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null)
        {
            return Result.NotFound($"Category {request.Id} not found");
        }

        if (category.IsMainCategory())
        {
            var hasSubCategories = await context.Set<Category>().AnyAsync(x => x.SubOfId == request.Id, cancellationToken);

            if (hasSubCategories)
                return Result.Error($"Category {request.Id} still using for sub categories");
        }

        context.Set<Category>().Remove(category);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
