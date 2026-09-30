namespace StarterKit.Modules.Identity.Authorization;

internal static class IdentityPermissions
{
    public const string Group = "identity";

    public static class Users
    {
        public const string View = $"{Group}.users.view";

        public const string Create = $"{Group}.users.create";

        public const string Update = $"{Group}.users.update";

        public const string Delete = $"{Group}.users.delete";
    }

    public static class Roles
    {
        public const string View = $"{Group}.roles.view";

        public const string Manage = $"{Group}.roles.manage";
    }
}
