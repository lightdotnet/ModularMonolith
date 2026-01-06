using FluentValidation;

namespace Monolith.Catalog;

public record ShopLookup : Pagination
{
    public string? Search { get; set; }
}

public class ShopDto : BaseDto
{
    public string Name { get; set; } = null!;

    public Status.ActiveStatus Status { get; set; }
}

public record CreateShopRequest
{
    public string Name { get; set; } = null!;
}

public class CreateShopRequestValidator : AbstractValidator<CreateShopRequest>
{
    public CreateShopRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);
    }
}