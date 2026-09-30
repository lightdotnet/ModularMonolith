namespace StarterKit.Modules.Identity.Authentication.ExternalLogin;

public interface IExternalLoginService
{
    Task<ExternalLoginOutcome> ResolveAsync(
        ExternalLoginDescriptor descriptor,
        CancellationToken cancellationToken = default);
}
