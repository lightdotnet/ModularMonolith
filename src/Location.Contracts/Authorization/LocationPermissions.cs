namespace StarterKit.Locations.Contracts.Authorization;

public static class LocationPermissions
{
    public const string Group = "location";

    public static class Locations
    {
        public const string View = $"{Group}.locations.view";

        public const string Manage = $"{Group}.locations.manage";
    }

    public static class LocationTypes
    {
        public const string View = $"{Group}.location_types.view";

        public const string Manage = $"{Group}.location_types.manage";
    }
}
