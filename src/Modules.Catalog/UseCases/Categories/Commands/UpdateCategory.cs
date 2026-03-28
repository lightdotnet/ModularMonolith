namespace Monolith.Catalog.UseCases.Categories.Commands;

public record UpdateCategoryCommand(CategoryDto Category)
    : ICommand<Result>;

internal class UpdateCategoryCommandHandler(CatalogContext context)
    : ICommandHandler<UpdateCategoryCommand, Result>
{
    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        await context.Set<Category>()
            .Where(new CategoryByIdSpec(request.Category.Id))
            .ExecuteUpdateAsync(x => x
                .SetProperty(c => c.Name, request.Category.Name)
                .SetProperty(c => c.Disable, request.Category.Disable),
                cancellationToken);

        return Result.Success();
    }
}
