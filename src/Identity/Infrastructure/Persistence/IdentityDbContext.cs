using Light.EventBus.Abstractions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using StarterKit.Modules.Identity.Application.Common;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Shared;

namespace StarterKit.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core context of the Identity module (schema <see cref="Schema"/>), built on the
/// ASP.NET Core Identity store.
/// </summary>
/// <remarks>
/// An asynchronous save runs in this order:
/// <list type="number">
///     <item>Audit fields are stamped on the tracked entries.</item>
///     <item>Domain events are dispatched in-process.</item>
///     <item>The changes are committed.</item>
///     <item>Only after a successful commit are the integration events buffered in the scoped
///     <see cref="IntegrationEventCollector"/> published through <see cref="IEventBus"/>.</item>
/// </list>
/// There is no outbox: a publish failure after the commit is logged and swallowed, so the
/// integration events of that save are lost. This risk is accepted; every event carries the
/// full current state plus a version, so a later event supersedes a lost one.
/// </remarks>
internal sealed class IdentityDbContext(
    ICurrentUser currentUser,
    IDateTime clock,
    IPublisher mediator,
    IntegrationEventCollector integrationEvents,
    IEventBus eventBus,
    ILogger<IdentityDbContext> logger,
    DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<User, Role, string, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>(options)
{
    public const string Schema = "identity";

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    /// <summary>
    /// Audits and saves synchronously. Domain events are not dispatched on this path, and
    /// integration events cannot be published from it; a synchronous save with buffered
    /// integration events is rejected rather than silently dropping them.
    /// </summary>
    // Overriding the bool overload also covers the parameterless SaveChanges().
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        if (integrationEvents.HasEvents)
        {
            throw new NotSupportedException(
                $"{nameof(IdentityDbContext)} has pending integration events; use SaveChangesAsync so they are published.");
        }

        this.AuditEntries(currentUser.UserId, clock.AuditTime, false);

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    // Overriding the (bool, CancellationToken) overload also covers SaveChangesAsync(CancellationToken).
    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        this.AuditEntries(currentUser.UserId, clock.AuditTime, false);

        await mediator.DispatchDomainEvents(this);

        int result;

        try
        {
            result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch
        {
            // Nothing was committed, so the collected events describe changes that did not happen.
            integrationEvents.Clear();
            throw;
        }

        await PublishIntegrationEventsAsync(cancellationToken);

        return result;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema(Schema);

        builder.BuildEntities();

        Database.FixSqliteDateTimeOffset(builder);
    }

    private async Task PublishIntegrationEventsAsync(CancellationToken cancellationToken)
    {
        if (!integrationEvents.HasEvents)
            return;

        try
        {
            await integrationEvents.PublishAllAsync(eventBus, cancellationToken);
        }
        catch (Exception ex)
        {
            // The commit already succeeded; failing the request now would misreport its outcome.
            logger.LogError(
                ex,
                "Publishing integration events failed after the Identity changes were committed; the events are lost.");
        }
        finally
        {
            integrationEvents.Clear();
        }
    }
}
