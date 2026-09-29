namespace StarterKit.Shared;

public interface ICurrentUser
{
    string? SessionId { get; }

    string? UserId { get; }

    string? Username { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);

    bool HasPermission(string permission);
}
