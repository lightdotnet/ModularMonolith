namespace StarterKit.Locations.Api.Domain.LocationTypes;

public class LocationTypeByIdSpec : Specification<LocationType>
{
    public LocationTypeByIdSpec(string locationTypeId)
    {
        Where(x => x.Id == locationTypeId);
    }
}
