using StarterKit.Locations.Contracts.Common;

namespace StarterKit.Locations.Contracts.Locations;

public class LocationDto : BaseDto
{
    public string? ParentLocationId { get; set; }

    public string LocationTypeId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public LocationStatus Status { get; set; }
}
