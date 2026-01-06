using FluentValidation;

namespace Monolith.Catalog;

public record CreateCategoryRequest
{
    public string Name { get; set; } = null!;

    public string? SubOfId { get; set; }
}

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);
    }
}

