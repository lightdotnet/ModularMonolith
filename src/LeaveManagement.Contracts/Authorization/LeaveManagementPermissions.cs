namespace StarterKit.LeaveManagement.Contracts.Authorization;

public static class LeaveManagementPermissions
{
    public const string Group = "leave";

    public static class Requests
    {
        public const string Manage = $"{Group}.requests.manage";
    }
}
