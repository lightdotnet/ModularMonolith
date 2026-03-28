namespace Monolith.Catalog.UseCases.Categories.Commands;

public record CreateCategoryCommand(CreateCategoryRequest Category)
    : ICommand<Result<string>>;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Category).SetValidator(new CreateCategoryRequestValidator());

        /*
        RuleFor(x => x.Category.Name)
            .NotEmpty()
            .MaximumLength(10);
        */
    }
}

internal class CreateCategoryCommandHandler(CatalogContext context)
    : ICommandHandler<CreateCategoryCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.Category.SubOfId))
        {
            var checkMainCategoryExising = await CheckExisting(request.Category.SubOfId);

            if (checkMainCategoryExising is false)
                return Result<string>.NotFound($"Category {request.Category.SubOfId} not found");
        }

        var entity = Category.Create(request.Category.Name, request.Category.SubOfId);

        await context.Set<Category>().AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(entity.Id);
    }

    public Task<bool> CheckExisting(string id)
        => context.Set<Category>()
        .Where(new CategoryByIdSpec(id).IsMainCategory().MustActive())
        .AnyAsync();
}
