using StarterKit.Locations.Contracts.Common;

namespace StarterKit.Locations.Contracts.LocationTypes;

public record UpdateLocationTypeRequest
{
    public string Name { get; set; } = null!;

    public string? AllowedParentTypeId { get; set; }

    public bool CanHaveChildren { get; set; }

    public LocationTypeStatus Status { get; set; }
}

public sealed class UpdateLocationTypeRequestValidator : AbstractValidator<UpdateLocationTypeRequest>
{
    public UpdateLocationTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AllowedParentTypeId).MaximumLength(450);
        RuleFor(x => x.Status).IsInEnum();
    }
}
