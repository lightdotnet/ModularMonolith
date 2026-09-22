namespace StarterKit.Catalog.Contracts.Products;

public record UpsertProductRequest
{
    public string CategoryId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Sku { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = null!;

    public decimal VatRate { get; set; }

    public IList<ProductImageDto> Images { get; set; } = [];
}

public sealed class UpsertProductRequestValidator : AbstractValidator<UpsertProductRequest>
{
    // SKU's 100-char cap is duplicated on the domain-side Sku value object and the EF column
    // mapping (CatalogDbContext) — Contracts cannot reference Catalog.Api's domain types, so this
    // mirrors the existing repo convention of independently duplicating a shape constraint across
    // the Contracts validator and the Api layer (e.g. Location's Name/Code max lengths).
    // Sku's NotEmpty rule is conditional (required only on create, not update) so it lives on
    // UpsertProductCommandValidator, not here.
    public UpsertProductRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Sku).MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        // Shape only: whether the code is a known, active currency is checked by the handler against
        // the Currency module.
        RuleFor(x => x.Currency)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency must be a three-letter ISO 4217 code.");
        RuleFor(x => x.VatRate).InclusiveBetween(0, 100);

        RuleForEach(x => x.Images).SetValidator(new UpsertProductImageRequestValidator());
    }
}

public sealed class UpsertProductImageRequestValidator : AbstractValidator<ProductImageDto>
{
    public UpsertProductImageRequestValidator()
    {
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048);
    }
}
