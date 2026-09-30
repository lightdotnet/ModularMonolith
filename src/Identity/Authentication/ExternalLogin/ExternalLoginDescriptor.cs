namespace StarterKit.Modules.Identity.Authentication.ExternalLogin;

public sealed record ExternalLoginDescriptor(
    string Provider,
    string ObjectId,
    string TenantId,
    string? Email,
    string? FirstName,
    string? LastName);
