namespace StarterKit.Locations.Contracts.Locations;

/// <summary>Thin projection for pickers/lookups — just enough to label a location in a list.</summary>
public class LocationLookupDto : BaseDto
{
    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string LocationTypeId { get; set; } = null!;
}
