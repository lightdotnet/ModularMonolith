using StarterKit.Catalog.Api.Data;
using StarterKit.Catalog.Api.Domain.Categories;

namespace StarterKit.Catalog.Api.Application.Categories.Commands;

internal sealed record UpdateCategoryCommand(string Id, UpdateCategoryRequest Model) : ICommand<IResult>;

internal sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Model).SetValidator(new UpdateCategoryRequestValidator());
    }
}

internal class UpdateCategoryCommandHandler(CatalogDbContext context)
    : ICommandHandler<UpdateCategoryCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var entity = await context.Categories
            .Where(new CategoryByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Category {request.Id} not found");

        var nameTaken = await context.Categories
            .AnyAsync(
                x => x.Id != request.Id && x.ParentCategoryId == entity.ParentCategoryId && x.Name == model.Name,
                cancellationToken);

        if (nameTaken)
            return Result.Error($"Category '{model.Name}' already exists under this parent.");

        entity.Rename(model.Name);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
