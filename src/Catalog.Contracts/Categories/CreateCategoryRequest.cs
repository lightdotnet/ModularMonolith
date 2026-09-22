namespace StarterKit.Catalog.Contracts.Categories;

public record CreateCategoryRequest
{
    public string? ParentCategoryId { get; set; }

    public string Name { get; set; } = null!;
}

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.ParentCategoryId).MaximumLength(450);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
