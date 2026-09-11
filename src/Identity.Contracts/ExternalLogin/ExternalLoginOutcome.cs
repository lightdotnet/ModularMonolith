namespace StarterKit.Identity.Contracts.ExternalLogin;

public sealed record ExternalLoginOutcome
{
    public required ExternalLoginStatus Status { get; init; }

    public string? UserId { get; init; }                       // Linked | Provisioned

    public ExternalLoginRejectionReason? Reason { get; init; } // Rejected
}

public enum ExternalLoginStatus
{
    Linked,
    Provisioned,
    Rejected,
}

public enum ExternalLoginRejectionReason
{
    MissingRequiredClaims,
    TenantNotAllowed,
    EmailDomainNotAllowed,
    EmailAlreadyRegistered,
    UserInactive,
    ProvisioningFailed,
}
