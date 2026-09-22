using StarterKit.Locations.Contracts.Common;

namespace StarterKit.Locations.Contracts.LocationTypes;

public class LocationTypeDto : BaseDto
{
    public string Name { get; set; } = null!;

    public string? AllowedParentTypeId { get; set; }

    public bool CanHaveChildren { get; set; }

    public LocationTypeStatus Status { get; set; }
}
