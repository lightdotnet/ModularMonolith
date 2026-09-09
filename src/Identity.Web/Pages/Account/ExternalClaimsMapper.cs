using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using StarterKit.Identity.Contracts.ExternalLogin;

namespace StarterKit.Identity.Web.Pages.Account;

/// <summary>
/// Owns every external-provider claim-type string. Turns the raw claims on an
/// <see cref="ExternalLoginInfo"/> into an <see cref="ExternalLoginDescriptor"/>, or
/// <c>null</c> when a required claim (object id / tenant id / email) is missing.
/// </summary>
internal static class ExternalClaimsMapper
{
    private const string ObjectIdClaimType = "oid";
    private const string LegacyObjectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
    private const string TenantIdClaimType = "tid";
    private const string LegacyTenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
    private const string EmailClaimType = "email";
    private const string PreferredUsernameClaimType = "preferred_username";
    private const string GivenNameClaimType = "given_name";
    private const string FamilyNameClaimType = "family_name";

    public static ExternalLoginDescriptor? ToDescriptor(ExternalLoginInfo info)
    {
        var principal = info.Principal;

        var objectId = principal.FindFirstValue(ObjectIdClaimType)
            ?? principal.FindFirstValue(LegacyObjectIdClaimType);

        var tenantId = principal.FindFirstValue(TenantIdClaimType)
            ?? principal.FindFirstValue(LegacyTenantIdClaimType);

        var email = principal.FindFirstValue(EmailClaimType)
            ?? principal.FindFirstValue(PreferredUsernameClaimType)
            ?? principal.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(objectId)
            || string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var firstName = principal.FindFirstValue(GivenNameClaimType)
            ?? principal.FindFirstValue(ClaimTypes.GivenName);

        var lastName = principal.FindFirstValue(FamilyNameClaimType)
            ?? principal.FindFirstValue(ClaimTypes.Surname);

        return new ExternalLoginDescriptor(
            info.LoginProvider,
            objectId,
            tenantId,
            email,
            firstName,
            lastName);
    }
}
