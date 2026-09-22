namespace StarterKit.Catalog.Contracts.Categories;

public record UpdateCategoryRequest
{
    public string Name { get; set; } = null!;
}

public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
