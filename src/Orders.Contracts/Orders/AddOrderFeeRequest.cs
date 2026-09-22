namespace StarterKit.Orders.Contracts.Orders;

public record AddOrderFeeRequest
{
    public string Name { get; set; } = null!;

    public decimal Amount { get; set; }

    public string FeeTypeId { get; set; } = null!;
}

public sealed class AddOrderFeeRequestValidator : AbstractValidator<AddOrderFeeRequest>
{
    public AddOrderFeeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FeeTypeId).NotEmpty().MaximumLength(450);
    }
}
