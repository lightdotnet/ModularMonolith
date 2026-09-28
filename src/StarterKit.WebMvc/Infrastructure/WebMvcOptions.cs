namespace StarterKit.WebMvc.Infrastructure;

/// <summary>
/// Server-to-server base URLs of the backend modules this host calls, bound from <c>Api</c>.
/// Each base URL owns its full path/version prefix (e.g. <c>http://localhost:5000/api/v1/</c>) —
/// request paths are resolved against it as-is, the same convention as the admin client's
/// <c>&lt;MODULE&gt;_API_BASE_URL</c> variables.
/// </summary>
public sealed class ApiOptions
{
    public const string SectionName = "Api";

    public ModuleApiOptions Identity { get; set; } = new();

    public ModuleApiOptions Notifications { get; set; } = new();
}

public sealed class ModuleApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Browser-reachable origin of <c>Identity.Web</c>, bound from <c>IdentityWeb</c>. The browser is
/// redirected there directly for the Microsoft external-login handshake, so unlike
/// <see cref="ApiOptions"/> (server-to-server) it must be reachable from the user's browser.
/// </summary>
public sealed class IdentityWebOptions
{
    public const string SectionName = "IdentityWeb";

    public string BaseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Absolute URL of the backend SignalR notification hub, bound from <c>SignalR</c>. Kept
/// server-side and handed to the browser at call time by the hub-token endpoint, so it stays
/// a runtime setting.
/// </summary>
public sealed class SignalROptions
{
    public const string SectionName = "SignalR";

    public string HubUrl { get; set; } = string.Empty;
}

/// <summary>
/// External-login UI switches, bound from <c>ExternalLogin</c>.
/// </summary>
public sealed class ExternalLoginOptions
{
    public const string SectionName = "ExternalLogin";

    /// <summary>Shows the "Sign in with Microsoft" button on the login page.</summary>
    public bool EnableMicrosoft { get; set; } = true;
}

/// <summary>
/// Permission-check settings, bound from <c>Authorization</c>.
/// </summary>
public sealed class PermissionOptions
{
    public const string SectionName = "Authorization";

    /// <summary>
    /// Usernames that bypass every permission check — the same rule as the admin client's
    /// <c>SUPER_ADMIN_USERNAMES</c>. Empty by default (base appsettings too); only
    /// <c>appsettings.Development.json</c> lists one, so a deployment opts in explicitly.
    /// </summary>
    public IList<string> SuperAdminUserNames { get; set; } = [];
}

/// <summary>
/// Data Protection key storage, bound from <c>DataProtection</c>. Ignored in Development (keys stay
/// in the default per-user location). Every other environment must set <see cref="KeysPath"/> to
/// a persistent directory shared by all instances — otherwise the session, antiforgery, TempData
/// and PKCE cookies become unreadable on every restart/scale-out — and should protect the keys at
/// rest with a certificate (<see cref="CertificateThumbprint"/> or <see cref="CertificatePath"/>).
/// </summary>
public sealed class DataProtectionKeyOptions
{
    public const string SectionName = "DataProtection";

    /// <summary>Directory the key ring is persisted to.</summary>
    public string? KeysPath { get; set; }

    /// <summary>Thumbprint of a certificate in the machine/user store used to encrypt the keys at rest.</summary>
    public string? CertificateThumbprint { get; set; }

    /// <summary>Path of a PFX file used to encrypt the keys at rest (alternative to <see cref="CertificateThumbprint"/>).</summary>
    public string? CertificatePath { get; set; }

    /// <summary>Password of the PFX at <see cref="CertificatePath"/>; keep it in a secret store, not appsettings.</summary>
    public string? CertificatePassword { get; set; }
}

/// <summary>
/// Reverse-proxy trust, bound from <c>ForwardedHeaders</c>. <c>X-Forwarded-For</c>/<c>-Proto</c> are
/// only honoured from these proxies/networks (loopback is always trusted); without them the client
/// IP (rate limiting) and scheme (Secure cookies, redirect URIs) would be the proxy's.
/// </summary>
public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>IP addresses of trusted proxies (e.g. <c>10.0.0.5</c>).</summary>
    public IList<string> KnownProxies { get; set; } = [];

    /// <summary>CIDR ranges of trusted proxies (e.g. <c>10.0.0.0/8</c>).</summary>
    public IList<string> KnownNetworks { get; set; } = [];
}

/// <summary>
/// Per-client-IP fixed-window limit on sign-in attempts (password POST and the Microsoft
/// external-login start), bound from <c>RateLimiting:SignIn</c>.
/// </summary>
public sealed class SignInRateLimitOptions
{
    public const string SectionName = "RateLimiting:SignIn";

    /// <summary>Attempts allowed per client IP within one window.</summary>
    public int PermitLimit { get; set; } = 10;

    /// <summary>Window length in seconds.</summary>
    public int WindowSeconds { get; set; } = 60;
}
