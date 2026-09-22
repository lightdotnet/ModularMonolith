namespace StarterKit.Locations.Api.Domain.Locations;

public class LocationByIdSpec : Specification<Location>
{
    public LocationByIdSpec(string locationId)
    {
        Where(x => x.Id == locationId);
    }
}
