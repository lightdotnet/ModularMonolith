using StarterKit.Orders.Contracts.Common;

namespace StarterKit.Orders.Contracts.Orders;

public record AddOrderFeeRequest
{
    public string Name { get; set; } = null!;

    public decimal Amount { get; set; }

    public OrderFeeType Type { get; set; }
}

public sealed class AddOrderFeeRequestValidator : AbstractValidator<AddOrderFeeRequest>
{
    public AddOrderFeeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Type).IsInEnum();
    }
}
