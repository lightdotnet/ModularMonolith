using Light.Domain.Entities.Interfaces;
using Light.Domain.ValueObjects;

namespace StarterKit.Persistence.Extensions;

public static class TrackingExtensions
{
    public static void AuditEntries<TContext>(this TContext context,
        string? userId,
        DateTimeOffset auditTime,
        bool enableSoftDelete = false)
        where TContext : DbContext
    {
        var changeTracker = context.ChangeTracker;

        // Handle owned ValueObjects.
        //
        // OwnsOne / table-split owned reference:
        //     IsUnique == true
        //
        // OwnsMany:
        //     IsUnique == false
        //     => real row delete, so leave untouched.
        foreach (var entry in changeTracker.Entries<ValueObject>())
        {
            if (entry.State == EntityState.Deleted &&
                entry.Metadata.FindOwnership() is { IsUnique: true })
            {
                entry.State = EntityState.Unchanged;
            }
        }

        // Handle soft delete first.
        //
        // Deleted -> Modified so that the entity will be UPDATEd instead
        // of DELETEd.
        if (enableSoftDelete)
        {
            foreach (var entry in changeTracker.Entries<ISoftDelete>())
            {
                if (entry.State != EntityState.Deleted)
                    continue;

                entry.Entity.Deleted = auditTime;
                entry.Entity.DeletedBy = userId;
                entry.State = EntityState.Modified;
            }
        }

        // Handle all audit-related entities in a single enumeration.
        foreach (var entry in changeTracker.Entries<IHasAudit>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    {
                        if (entry.Entity is IHasCreationTime creationTime)
                        {
                            creationTime.Created = auditTime;
                        }

                        if (entry.Entity is IHasAuditUser auditUser)
                        {
                            auditUser.CreatedBy = userId;
                        }

                        break;
                    }

                case EntityState.Modified:
                    {
                        if (entry.Entity is IHasModificationTime modificationTime)
                        {
                            modificationTime.LastModified = auditTime;
                        }

                        if (entry.Entity is IHasAuditUser auditUser)
                        {
                            auditUser.LastModifiedBy = userId;
                        }

                        break;
                    }
            }
        }
    }
}
