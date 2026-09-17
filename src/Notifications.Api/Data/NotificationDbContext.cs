using Microsoft.EntityFrameworkCore;
using StarterKit.Notifications.Api.Entities;
using StarterKit.Persistence.Context;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;

namespace StarterKit.Notifications.Api.Data;

public class NotificationDbContext(
    ICurrentUser currentUser,
    IDateTime clock,
    DbContextOptions<NotificationDbContext> options) :
    BaseDbContext(options)
{
    public const string Schema = "system";

    public virtual DbSet<Notification> Notifications => Set<Notification>();

    public override int SaveChanges()
    {
        this.AuditEntries(currentUser.UserId, clock.AuditTime, false);
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.AuditEntries(currentUser.UserId, clock.AuditTime, false);
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void ConfigureModel(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);

        builder.Entity<Notification>(entity =>
        {
            entity.ToTable(name: "Notifications");

            entity.HasIndex(x => x.ToUserId);

            entity.ConfigureAuditableEntity();

            entity.Property(x => x.FromUserId).HasMaxLength(450);

            entity.Property(x => x.FromName).HasMaxLength(200);

            entity.Property(x => x.ToUserId).HasMaxLength(450);

            entity.Property(x => x.Title).HasMaxLength(250);
        });
    }
}
