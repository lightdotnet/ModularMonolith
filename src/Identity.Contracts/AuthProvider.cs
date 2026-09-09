namespace StarterKit.Identity.Contracts;

public enum AuthProvider
{
    Local = 0,
    ActiveDirectory = 1,   // legacy stored value "AD"
    EntraId = 2,           // the OIDC Microsoft provider (legacy stored value "Microsoft")
}
