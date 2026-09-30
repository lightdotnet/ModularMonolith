using StarterKit.Modules.Notifications.Domain;
using StarterKit.Persistence.Context;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;

namespace StarterKit.Modules.Notifications.Persistence;

/// <summary>
/// EF Core context of the Notifications module (schema <see cref="Schema"/>). Audit fields are
/// stamped on every save; the module raises no domain or integration events.
/// </summary>
internal sealed class NotificationDbContext(
    ICurrentUser currentUser,
    IDateTime clock,
    DbContextOptions<NotificationDbContext> options)
    : BaseDbContext(options)
{
    public const string Schema = "system";

    public DbSet<Notification> Notifications => Set<Notification>();

    // Overriding the bool overload also covers the parameterless SaveChanges().
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.AuditEntries(currentUser.UserId, clock.AuditTime, false);

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    // Overriding the (bool, CancellationToken) overload also covers SaveChangesAsync(CancellationToken).
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        this.AuditEntries(currentUser.UserId, clock.AuditTime, false);

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void ConfigureModel(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);

        builder.Entity<Notification>(entity =>
        {
            entity.ToTable(name: "Notifications");

            entity.HasIndex(x => x.RecipientUserId);

            entity.ConfigureAuditableEntity();

            entity.Property(x => x.RecipientUserId).HasMaxLength(450);

            entity.Property(x => x.Title).HasMaxLength(250);

            entity.Property(x => x.SenderUserId).HasMaxLength(450);

            entity.Property(x => x.SenderName).HasMaxLength(200);
        });
    }
}
