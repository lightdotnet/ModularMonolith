using Light.Domain.Entities.Interfaces;
using Light.Domain.ValueObjects;

namespace StarterKit.Persistence.Extensions;

public static class TrackingExtensions
{
    public static void AuditEntries<TContext>(this TContext context, string? userId, DateTimeOffset auditTime, bool enableSoftDelete = false)
        where TContext : DbContext
    {
        var changeTracker = context.ChangeTracker;

        // fix null value when delete for Entities inherited ISoftDelete & ValueObjects: only for
        // table-split owned references (OwnsOne, e.g. User.Status/Product.Price/Product.VatRate) —
        // FindOwnership().IsUnique is true there and false for an owned collection (OwnsMany, e.g.
        // Product.Images), where Deleted is a real row delete that must go through untouched.
        changeTracker.Entries<ValueObject>()
            .Where(x => x.State is EntityState.Deleted && x.Metadata.FindOwnership() is { IsUnique: true })
            .ToList()
            .ForEach(e => e.State = EntityState.Unchanged);

        if (enableSoftDelete)
        {
            // auto set Deleted & DeletedBy for Entities inherited ISoftDelete
            changeTracker.Entries<ISoftDelete>()
                .Where(x => x.State is EntityState.Deleted)
                .ToList()
                .ForEach(e =>
                {
                    e.Entity.Deleted = auditTime;
                    e.Entity.DeletedBy = userId;
                    e.State = EntityState.Modified;
                });
        }

        // tracking creation time for Entities inherited IHasCreationTime
        changeTracker.Entries<IHasCreationTime>()
            .Where(x => x.State is EntityState.Added)
            .ToList()
            .ForEach(e =>
            {
                e.Entity.Created = auditTime;
            });

        // tracking modification time for Entities inherited IHasModificationTime
        changeTracker.Entries<IHasModificationTime>()
            .Where(x => x.State is EntityState.Modified)
            .ToList()
            .ForEach(e =>
            {
                e.Entity.LastModified = auditTime;
            });

        // tracking users actions for Entities inherited IHasAuditUser
        changeTracker.Entries<IHasAuditUser>()
            .Where(x => x.State is EntityState.Added or EntityState.Modified)
            .ToList()
            .ForEach(e =>
            {
                switch (e.State)
                {
                    case EntityState.Added:
                        e.Entity.CreatedBy = userId;
                        break;
                    case EntityState.Modified:
                        e.Entity.LastModifiedBy = userId;
                        break;
                }
            });
    }
}
