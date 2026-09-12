using StarterKit.Catalog.Api.Data;
using StarterKit.Catalog.Api.Domain.Categories;

namespace StarterKit.Catalog.Api.Application.Categories.Commands;

internal sealed record MoveCategoryCommand(string Id, MoveCategoryRequest Model) : ICommand<IResult>;

internal sealed class MoveCategoryCommandValidator : AbstractValidator<MoveCategoryCommand>
{
    public MoveCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Model).SetValidator(new MoveCategoryRequestValidator());
    }
}

internal class MoveCategoryCommandHandler(CatalogDbContext context)
    : ICommandHandler<MoveCategoryCommand, IResult>
{
    public async Task<IResult> Handle(
        MoveCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Categories
            .Where(new CategoryByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Category {request.Id} not found");

        var newParentId = request.Model.NewParentCategoryId;

        if (string.IsNullOrEmpty(newParentId))
        {
            entity.Move(null);

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        if (newParentId == entity.Id)
            return Result.Error("A category cannot be its own parent.");

        var newParentExists = await context.Categories
            .AnyAsync(x => x.Id == newParentId, cancellationToken);

        if (!newParentExists)
            return Result.NotFound($"Category {newParentId} not found");

        var isDescendant = await IsDescendantAsync(context, entity.Id, newParentId, cancellationToken);

        if (isDescendant)
            return Result.Error("Cannot move a category under one of its own descendants.");

        entity.Move(newParentId);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static async Task<bool> IsDescendantAsync(
        CatalogDbContext context, string ancestorId, string candidateId, CancellationToken cancellationToken)
    {
        var currentId = candidateId;

        while (!string.IsNullOrEmpty(currentId))
        {
            if (currentId == ancestorId)
                return true;

            currentId = await context.Categories
                .Where(new CategoryByIdSpec(currentId))
                .Select(x => x.ParentCategoryId)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return false;
    }
}
