namespace StarterKit.Modules.ModuleName.Application.Common;

/// <summary>
/// The module's store as the use cases see it, so the handlers do not depend on the
/// infrastructure context type. Expose <c>DbSet&lt;T&gt;</c> properties here as the domain grows.
/// </summary>
public interface IApplicationContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
