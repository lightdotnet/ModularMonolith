namespace StarterKit.Modules.Identity.Application.Authentication.ExternalLogin;

public sealed record ExternalLoginDescriptor(
    string Provider,
    string ObjectId,
    string TenantId,
    string? Email,
    string? FirstName,
    string? LastName);
