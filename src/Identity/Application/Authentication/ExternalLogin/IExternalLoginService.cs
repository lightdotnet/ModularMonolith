namespace StarterKit.Modules.Identity.Application.Authentication.ExternalLogin;

public interface IExternalLoginService
{
    Task<ExternalLoginOutcome> ResolveAsync(
        ExternalLoginDescriptor descriptor,
        CancellationToken cancellationToken = default);
}
