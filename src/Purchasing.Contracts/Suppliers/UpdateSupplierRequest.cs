namespace StarterKit.Purchasing.Contracts.Suppliers;

public record UpdateSupplierRequest
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? ContactName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? PaymentTerms { get; set; }
}

public sealed class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierRequestValidator()
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
