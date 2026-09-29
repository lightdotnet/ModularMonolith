namespace StarterKit.Shared.Authorization;

/// <summary>
/// Default user with full permissions.
/// </summary>
public sealed class SuperUserPolicy
{
    public const string SuperUserName = "super";

    // Add project-specific super-users here, or better, source them from config/claims instead of hardcoding.
    private static List<string> SuperUsers =>
    [
        SuperUserName
    ];

    public static bool IsSuper(string? userName) => SuperUsers.Any(x => x == userName);
}
