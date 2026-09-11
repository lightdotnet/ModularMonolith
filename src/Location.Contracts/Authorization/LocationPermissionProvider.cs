using Light.AspNetCore.Authorization;

namespace StarterKit.Locations.Contracts.Authorization;

public class LocationPermissionProvider : IPermissionDefinitionProvider
{
    public IEnumerable<PermissionDefinition> Define()
    {
        yield return new(
            LocationPermissions.Locations.View,
            "View Locations",
            LocationPermissions.Group);

        yield return new(
            LocationPermissions.Locations.Manage,
            "Manage Locations",
            LocationPermissions.Group);

        yield return new(
            LocationPermissions.LocationTypes.View,
            "View Location Types",
            LocationPermissions.Group);

        yield return new(
            LocationPermissions.LocationTypes.Manage,
            "Manage Location Types",
            LocationPermissions.Group);
    }
}
