using StarterKit.Shared;

namespace Organization.Tests.TestSupport;

public sealed class FakeCurrentUser : ICurrentUser
{
    public string? SessionId { get; set; }

    public string? UserId { get; set; }

    public string? Username { get; set; }

    public bool IsAuthenticated { get; set; }

    public HashSet<string> Roles { get; } = [];

    public HashSet<string> Permissions { get; } = [];

    public bool IsInRole(string role) => Roles.Contains(role);

    public bool HasPermission(string permission) => Permissions.Contains(permission);
}
