using StarterKit.Shared.Constants;

namespace StarterKit.Catalog.Contracts.Products;

/// <summary>Mirrors <see cref="CreateProductRequest"/> minus <c>Sku</c> — immutable post-create, same as <c>Location.Code</c>.</summary>
public record UpdateProductRequest
{
    public string CategoryId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = null!;

    public decimal VatRate { get; set; }
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Equal(CurrencyConstants.Default);
        RuleFor(x => x.VatRate).InclusiveBetween(0, 100);
    }
}
