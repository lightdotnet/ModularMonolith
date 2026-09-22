using StarterKit.Orders.Contracts.Common;

namespace StarterKit.Orders.Contracts.OrderTypes;

public record CreateOrderTypeRequest
{
    public string Id { get; set; } = null!;

    public OrderTypeCategory Category { get; set; }

    public string Name { get; set; } = null!;
}

public sealed class CreateOrderTypeRequestValidator : AbstractValidator<CreateOrderTypeRequest>
{
    public CreateOrderTypeRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty().MaximumLength(450);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
