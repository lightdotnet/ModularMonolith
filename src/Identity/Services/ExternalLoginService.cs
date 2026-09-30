using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using StarterKit.Modules.Identity.Authentication.ExternalLogin;
using StarterKit.Modules.Identity.Contracts.IntegrationEvents;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.IntegrationEvents;
using StarterKit.Modules.Identity.Models;
using StarterKit.Shared;

namespace StarterKit.Modules.Identity.Services;

/// <summary>
/// Resolves an external (OIDC) sign-in to a local user: links a known provider identity,
/// or just-in-time provisions a passwordless local user when the tenant and email domain
/// are allow-listed. Never auto-links to a pre-existing local account matched only by email
/// (the nOAuth class of attack).
/// </summary>
internal sealed class ExternalLoginService(
    UserManager<User> userManager,
    IOptions<ExternalLoginOptions> options,
    IntegrationEventCollector integrationEvents,
    IDateTime clock,
    ILogger<ExternalLoginService> logger)
    : IExternalLoginService
{
    private readonly ExternalLoginOptions _options = options.Value;

    public async Task<ExternalLoginOutcome> ResolveAsync(
        ExternalLoginDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(descriptor.ObjectId)
            || string.IsNullOrWhiteSpace(descriptor.TenantId)
            || string.IsNullOrWhiteSpace(descriptor.Email))
        {
            logger.LogWarning("External login rejected: required claims missing.");
            return Rejected(ExternalLoginRejectionReason.MissingRequiredClaims);
        }

        if (!_options.AllowedTenantIds.Contains(descriptor.TenantId, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "External login rejected: tenant {TenantId} is not allow-listed.",
                descriptor.TenantId);
            return Rejected(ExternalLoginRejectionReason.TenantNotAllowed);
        }

        var providerKey = $"{descriptor.TenantId}|{descriptor.ObjectId}";

        var linkedUser = await userManager.FindByLoginAsync(descriptor.Provider, providerKey);
        if (linkedUser is not null)
        {
            if (linkedUser.Status.IsActive is false || linkedUser.Deleted is not null)
            {
                logger.LogWarning(
                    "External login rejected: linked user {UserId} is inactive or deleted.",
                    linkedUser.Id);
                return Rejected(ExternalLoginRejectionReason.UserInactive);
            }

            return new ExternalLoginOutcome
            {
                Status = ExternalLoginStatus.Linked,
                UserId = linkedUser.Id,
            };
        }

        var byEmail = await userManager.FindByEmailAsync(descriptor.Email);
        if (byEmail is not null)
        {
            logger.LogWarning(
                "External login rejected: email {Email} is already registered to user {UserId}; no auto-link.",
                descriptor.Email,
                byEmail.Id);
            return Rejected(ExternalLoginRejectionReason.EmailAlreadyRegistered);
        }

        var emailDomain = EmailDomainOf(descriptor.Email);
        if (emailDomain is null
            || !_options.AllowedEmailDomains.Contains(emailDomain, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "External login rejected: email domain '{Domain}' is not allow-listed.",
                emailDomain);
            return Rejected(ExternalLoginRejectionReason.EmailDomainNotAllowed);
        }

        var newUser = User.ProvisionFromExternalIdentity(
            descriptor.Email,
            descriptor.FirstName,
            descriptor.LastName,
            AuthProvider.EntraId);

        var create = await userManager.CreateAsync(newUser);
        if (!create.Succeeded)
        {
            logger.LogError(
                "External login provisioning failed for {Email}: {Errors}",
                descriptor.Email,
                string.Join("; ", create.Errors.Select(e => $"{e.Code}:{e.Description}")));
            return Rejected(ExternalLoginRejectionReason.ProvisioningFailed);
        }

        // Buffered before the login link is saved, so it is published only once the user is
        // both created and linked - a provisioning rolled back below never announces the user.
        integrationEvents.Add(UserIntegrationEvents.Provisioned(
            newUser,
            ProvisioningSource.ExternalLogin,
            UserIntegrationEvents.NextVersion(clock)));

        var link = await userManager.AddLoginAsync(
            newUser,
            new UserLoginInfo(
                descriptor.Provider,
                providerKey,
                descriptor.Provider));
        if (!link.Succeeded)
        {
            integrationEvents.Clear();

            logger.LogError(
                "External login link failed for {Email}: {Errors}. Rolling back the provisioned user.",
                descriptor.Email,
                string.Join("; ", link.Errors.Select(e => $"{e.Code}:{e.Description}")));

            var rollback = await userManager.DeleteAsync(newUser);
            if (!rollback.Succeeded)
            {
                logger.LogError(
                    "Failed to roll back provisioned user {UserId} after a failed login link.",
                    newUser.Id);
            }

            return Rejected(ExternalLoginRejectionReason.ProvisioningFailed);
        }

        return new ExternalLoginOutcome
        {
            Status = ExternalLoginStatus.Provisioned,
            UserId = newUser.Id,
        };
    }

    private static ExternalLoginOutcome Rejected(ExternalLoginRejectionReason reason) => new()
    {
        Status = ExternalLoginStatus.Rejected,
        Reason = reason,
    };

    private static string? EmailDomainOf(string email)
    {
        var at = email.LastIndexOf('@');
        if (at < 0 || at == email.Length - 1)
            return null;

        return email[(at + 1)..].ToLowerInvariant();
    }
}
