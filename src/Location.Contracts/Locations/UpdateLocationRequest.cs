using StarterKit.Locations.Contracts.Common;

namespace StarterKit.Locations.Contracts.Locations;

public record UpdateLocationRequest
{
    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public LocationStatus Status { get; set; }
}

public sealed class UpdateLocationRequestValidator : AbstractValidator<UpdateLocationRequest>
{
    public UpdateLocationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Status).IsInEnum();
    }
}
