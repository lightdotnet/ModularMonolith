using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StarterKit.Shared.Entities;

namespace StarterKit.Persistence.Extensions;

public static class EntityTypeBuilderExtensions
{
    public static void ConfigureAuditableEntity<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity
    {
        builder.Property(x => x.Id).HasMaxLength(450);

        builder.Property(x => x.CreatedBy).HasMaxLength(450);

        builder.Property(x => x.LastModifiedBy).HasMaxLength(450);
    }

    // Overload for the database-generated-key base (AuditableEntity<TId>, e.g. bigint IDENTITY
    // PKs) — Id needs no explicit configuration here (no app-assigned string length to cap; the
    // provider's own numeric-key convention takes over), only the two audit-user columns.
    public static void ConfigureAuditableEntity<TEntity, TId>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity<TId>
    {
        builder.Property(x => x.CreatedBy).HasMaxLength(450);

        builder.Property(x => x.LastModifiedBy).HasMaxLength(450);
    }
}
