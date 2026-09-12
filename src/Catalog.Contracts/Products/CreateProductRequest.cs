using StarterKit.Shared.Constants;

namespace StarterKit.Catalog.Contracts.Products;

public record CreateProductRequest
{
    public string CategoryId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Sku { get; set; } = null!;

    public decimal Price { get; set; }

    public string Currency { get; set; } = null!;

    public decimal VatRate { get; set; }
}

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    // SKU's 100-char cap is duplicated on the domain-side Sku value object and the EF column
    // mapping (CatalogDbContext) — Contracts cannot reference Catalog.Api's domain types, so this
    // mirrors the existing repo convention of independently duplicating a shape constraint across
    // the Contracts validator and the Api layer (e.g. Location's Name/Code max lengths).
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Equal(CurrencyConstants.Default);
        RuleFor(x => x.VatRate).InclusiveBetween(0, 100);
    }
}
