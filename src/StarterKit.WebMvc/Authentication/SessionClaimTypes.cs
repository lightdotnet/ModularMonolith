using StarterKit.Shared.Constants;

namespace StarterKit.WebMvc.Authentication;

/// <summary>
/// Claim types on the cookie principal. JWT-sourced types reuse the backend's
/// <see cref="ClaimTypeConstants"/>; profile-sourced ones are added at sign-in/refresh.
/// </summary>
public static class SessionClaimTypes
{
    public const string UserId = ClaimTypeConstants.UserId;

    public const string UserName = ClaimTypeConstants.UserName;

    public const string FirstName = ClaimTypeConstants.FirstName;

    public const string LastName = ClaimTypeConstants.LastName;

    public const string FullName = ClaimTypeConstants.FullName;

    public const string Email = ClaimTypeConstants.Email;

    public const string PhoneNumber = ClaimTypeConstants.PhoneNumber;

    public const string Role = ClaimTypeConstants.Role;

    public const string Permission = ClaimTypeConstants.Permission;

    public const string EmployeeId = ClaimTypeConstants.EmployeeId;

    /// <summary>JWT id (<c>jti</c>) — the backend session id, used to revoke the session on logout.</summary>
    public const string TokenId = ClaimTypeConstants.TokenId;

    /// <summary>Profile-sourced: the user's auth provider, as reported by <c>GET user_profile</c>.</summary>
    public const string AuthProvider = "auth_provider";
}
