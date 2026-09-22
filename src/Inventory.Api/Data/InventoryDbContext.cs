using StarterKit.Inventory.Api.Domain.StockAdjustments;
using StarterKit.Inventory.Api.Domain.StockLevels;
using StarterKit.Persistence.Context;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;

namespace StarterKit.Inventory.Api.Data;

public class InventoryDbContext(
    ICurrentUser currentUser,
    IDateTime clock,
    DbContextOptions<InventoryDbContext> options) :
    BaseDbContext(options)
{
    public const string Schema = "inventory";

    public virtual DbSet<StockLevel> StockLevels => Set<StockLevel>();

    public virtual DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();

    public override int SaveChanges()
    {
        this.AuditEntries(currentUser.UserId, clock.AuditTime, false);
        RotateConcurrencyTokens();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.AuditEntries(currentUser.UserId, clock.AuditTime, false);
        RotateConcurrencyTokens();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void ConfigureModel(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);

        builder.Entity<StockLevel>(entity =>
        {
            entity.ToTable(name: "StockLevels");

            entity.HasIndex(x => new { x.ProductId, x.LocationId }).IsUnique();

            entity.Property(x => x.ConcurrencyToken)
                .IsConcurrencyToken()
                .HasMaxLength(32)
                .IsRequired();

            entity.ConfigureAuditableEntity<StockLevel, long>();

            entity.Property(x => x.LocationId).HasMaxLength(450);

            entity.Property(x => x.TotalValueBase).HasPrecision(19, 4);
        });

        builder.Entity<StockAdjustment>(entity =>
        {
            entity.ToTable(name: "StockAdjustments");

            entity.HasIndex(x => new { x.SourceType, x.SourceId });

            // Supports the "not reversed" anti-join used by reversal and reconciliation queries.
            entity.HasIndex(x => x.ReversesAdjustmentId);

            entity.HasIndex(x => new { x.ProductId, x.LocationId });

            // Nullable unique column — SQL Server's EF provider adds the "IS NOT NULL" filter itself,
            // so any number of rows may carry no key (same as Employee.UserId).
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();

            entity.ConfigureAuditableEntity<StockAdjustment, long>();

            entity.Property(x => x.LocationId).HasMaxLength(450);

            entity.Property(x => x.Note).HasMaxLength(500);

            entity.Property(x => x.PerformedByUserId).HasMaxLength(450);

            entity.Property(x => x.IdempotencyKey).HasMaxLength(200);

            entity.Property(x => x.UnitCostBase).HasPrecision(19, 4);

            entity.Property(x => x.ValueDeltaBase).HasPrecision(19, 4);
        });
    }

    private void RotateConcurrencyTokens()
    {
        foreach (var entry in ChangeTracker.Entries<StockLevel>()
            .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.RotateConcurrencyToken();
        }
    }
}
