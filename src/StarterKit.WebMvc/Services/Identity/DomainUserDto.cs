namespace StarterKit.WebMvc.Services.Identity;

/// <summary>
/// Active Directory lookup result returned by <c>GET user/get_domain_user/{userName}</c>. The
/// backend returns the vendor <c>Light.ActiveDirectory</c> DTO, which is not part of any module's
/// Contracts, so this host keeps its own read model of the fields it uses.
/// </summary>
public sealed record DomainUserDto
{
    public string UserName { get; init; } = string.Empty;

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Email { get; init; }

    public string? PhoneNumber { get; init; }
}
