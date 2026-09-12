using StarterKit.Catalog.Api.Data;
using StarterKit.Catalog.Api.Domain.Categories;

namespace StarterKit.Catalog.Api.Application.Categories.Commands;

internal sealed record CreateCategoryCommand(CreateCategoryRequest Model) : ICommand<IResult<string>>;

internal sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Model).SetValidator(new CreateCategoryRequestValidator());
    }
}

internal class CreateCategoryCommandHandler(CatalogDbContext context)
    : ICommandHandler<CreateCategoryCommand, IResult<string>>
{
    public async Task<IResult<string>> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        if (!string.IsNullOrEmpty(model.ParentCategoryId))
        {
            var parentExists = await context.Categories
                .AnyAsync(x => x.Id == model.ParentCategoryId, cancellationToken);

            if (!parentExists)
                return Result<string>.NotFound($"Category {model.ParentCategoryId} not found");
        }

        // Known gap: a unique (ParentCategoryId, Name) DB index treats two NULL ParentCategoryId
        // rows as distinct, so this pre-check is the only guard against duplicate root-level names —
        // see Category's class doc.
        var nameTaken = await context.Categories
            .AnyAsync(
                x => x.ParentCategoryId == model.ParentCategoryId && x.Name == model.Name,
                cancellationToken);

        if (nameTaken)
            return Result<string>.Error($"Category '{model.Name}' already exists under this parent.");

        var entity = Category.Create(model.Name, model.ParentCategoryId);

        await context.Categories.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(entity.Id);
    }
}
