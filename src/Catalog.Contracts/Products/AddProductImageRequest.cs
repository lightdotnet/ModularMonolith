namespace StarterKit.Catalog.Contracts.Products;

public record AddProductImageRequest
{
    public string Url { get; set; } = null!;

    public int? SortOrder { get; set; }
}

public sealed class AddProductImageRequestValidator : AbstractValidator<AddProductImageRequest>
{
    public AddProductImageRequestValidator()
    {
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048);
    }
}
