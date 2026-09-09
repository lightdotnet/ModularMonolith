namespace StarterKit.Identity.Contracts.ExternalLogin;

public interface IExternalLoginService
{
    Task<ExternalLoginOutcome> ResolveAsync(
        ExternalLoginDescriptor descriptor,
        CancellationToken cancellationToken = default);
}
