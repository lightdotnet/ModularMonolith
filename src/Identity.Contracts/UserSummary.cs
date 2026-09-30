namespace StarterKit.Modules.Identity.Contracts;

/// <summary>
/// Read-only snapshot of a user, exposed to other modules through <see cref="IIdentityModuleApi"/>.
/// </summary>
public sealed record UserSummary(
    string Id,
    string UserName,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsActive);
