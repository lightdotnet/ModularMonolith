namespace StarterKit.Purchasing.Contracts.Suppliers;

public record CreateSupplierRequest
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? ContactName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? PaymentTerms { get; set; }
}

public sealed class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactName).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Email).MaximumLength(256);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.PaymentTerms).MaximumLength(500);
    }
}
