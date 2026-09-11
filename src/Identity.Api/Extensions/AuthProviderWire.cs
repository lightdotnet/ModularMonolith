using StarterKit.Identity.Contracts;

namespace StarterKit.Identity.Api.Extensions;

/// <summary>
/// Bridges the <see cref="AuthProvider"/> enum to the legacy string values carried on the
/// <c>UserDto</c> / <c>CreateUserRequest</c> wire contract: <c>null</c> for a local account,
/// <c>"AD"</c> for Active Directory, <c>"Microsoft"</c> for the OIDC Entra ID provider.
/// </summary>
internal static class AuthProviderWire
{
    public const string ActiveDirectory = "AD";

    public const string EntraId = "Microsoft";

    public static AuthProvider Parse(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "ad" or "activedirectory" => AuthProvider.ActiveDirectory,
        "microsoft" or "entraid" => AuthProvider.EntraId,
        _ => AuthProvider.Local,
    };
}
