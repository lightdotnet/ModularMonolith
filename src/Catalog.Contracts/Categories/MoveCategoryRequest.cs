namespace StarterKit.Catalog.Contracts.Categories;

public record MoveCategoryRequest
{
    public string? NewParentCategoryId { get; set; }
}

public sealed class MoveCategoryRequestValidator : AbstractValidator<MoveCategoryRequest>
{
    public MoveCategoryRequestValidator()
    {
        RuleFor(x => x.NewParentCategoryId).MaximumLength(450);
    }
}
