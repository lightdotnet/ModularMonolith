using Light.Mediator;
using Microsoft.Extensions.Logging;
using StarterKit.Persistence.Context;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;
using StarterKit.Transfers.Api.Domain.StockTransfers;

namespace StarterKit.Transfers.Api.Data;

public class TransfersDbContext(
    ICurrentUser currentUser,
    IDateTime clock,
    IPublisher publisher,
    ILogger<TransfersDbContext> logger,
    DbContextOptions<TransfersDbContext> options) :
    BaseDbContext(options)
{
    public const string Schema = "transfers";

    public virtual DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();

    public virtual DbSet<TransferLine> TransferLines => Set<TransferLine>();

    public virtual DbSet<TransferReceipt> TransferReceipts => Set<TransferReceipt>();

    public virtual DbSet<TransferReceiptLine> TransferReceiptLines => Set<TransferReceiptLine>();

    public override int SaveChanges()
    {
        // The Transfers write path is 100% async (every handler only ever calls SaveChangesAsync), so
        // this override stays audit-only and does not dispatch domain events — mirrors OrdersDbContext.
        this.AuditEntries(currentUser.UserId, clock.AuditTime, false);
        RotateConcurrencyTokens();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.AuditEntries(currentUser.UserId, clock.AuditTime, false);
        RotateConcurrencyTokens();

        var result = await base.SaveChangesAsync(cancellationToken);

        // A downstream domain-event handler fault must never fail an already-committed write.
        try
        {
            await publisher.DispatchDomainEvents(this);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "One or more Transfers domain-event handlers threw after SaveChangesAsync; the write is already committed.");
        }

        return result;
    }

    protected override void ConfigureModel(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);

        builder.Entity<StockTransfer>(entity =>
        {
            entity.ToTable(name: "StockTransfers");

            entity.HasIndex(x => x.TransferCode).IsUnique();

            entity.HasIndex(x => x.SourceLocationId);

            entity.HasIndex(x => x.DestinationLocationId);

            entity.HasIndex(x => new { x.Status, x.Created });

            // Supports the posting reconciliation sweep, which only looks at transfers stuck in Posting.
            entity.HasIndex(x => x.PostingStartedAt)
                .HasProviderFilter(
                    Database,
                    "[PostingStartedAt] IS NOT NULL",
                    "\"PostingStartedAt\" IS NOT NULL");

            entity.Property(x => x.ConcurrencyToken)
                .IsConcurrencyToken()
                .HasMaxLength(32)
                .IsRequired();

            entity.ConfigureAuditableEntity<StockTransfer, long>();

            // Converted scalar column, not an owned type — same reasoning as Orders' OrderCode.
            entity.Property(x => x.TransferCode)
                .HasConversion(code => code.Value, v => new TransferCode(v))
                .HasMaxLength(TransferCode.MaxLength);

            entity.Property(x => x.SourceLocationId).HasMaxLength(450);

            entity.Property(x => x.SourceLocationName).HasMaxLength(200);

            entity.Property(x => x.DestinationLocationId).HasMaxLength(450);

            entity.Property(x => x.DestinationLocationName).HasMaxLength(200);

            entity.Property(x => x.Note).HasMaxLength(1000);

            entity.Property(x => x.ClosedReason).HasMaxLength(1000);

            entity.Property(x => x.CancelledReason).HasMaxLength(1000);

            entity.HasMany(x => x.Lines)
                .WithOne(x => x.Transfer)
                .HasForeignKey(x => x.TransferId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(x => x.Receipts)
                .WithOne(x => x.Transfer)
                .HasForeignKey(x => x.TransferId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(x => x.Receipts).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<TransferLine>(entity =>
        {
            entity.ToTable(name: "TransferLines");

            entity.HasIndex(x => x.TransferId);

            entity.HasIndex(x => x.ProductId);

            entity.ConfigureAuditableEntity<TransferLine, long>();

            entity.Property(x => x.ProductName).HasMaxLength(200);

            entity.Property(x => x.Sku).HasMaxLength(100);

            entity.Property(x => x.UnitCostBase).HasPrecision(19, 4);

            entity.Ignore(x => x.QtyInTransit);

            entity.Ignore(x => x.ClosedShortValueBase);
        });

        builder.Entity<TransferReceipt>(entity =>
        {
            entity.ToTable(name: "TransferReceipts");

            entity.HasIndex(x => new { x.TransferId, x.ClientRequestId }).IsUnique();

            // Supports the posting reconciliation sweep, which only looks at receipts stuck in Posting.
            entity.HasIndex(x => new { x.Status, x.Created });

            entity.ConfigureAuditableEntity<TransferReceipt, long>();

            entity.Property(x => x.ClientRequestId).HasMaxLength(100);

            entity.Property(x => x.VoidReason).HasMaxLength(1000);

            entity.HasMany(x => x.Lines)
                .WithOne(x => x.Receipt)
                .HasForeignKey(x => x.ReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<TransferReceiptLine>(entity =>
        {
            entity.ToTable(name: "TransferReceiptLines");

            entity.HasIndex(x => x.ReceiptId);

            entity.ConfigureAuditableEntity<TransferReceiptLine, long>();
        });
    }

    /// <summary>
    /// Rotates the token of every transfer that is itself modified <em>or</em> whose lines/receipts are
    /// added/changed/removed. Orders only rotates on the root's own changes; here a receipt is a pure
    /// child insert, and two concurrent receipts must still collide on the parent's token or they could
    /// both pass the over-receipt guard.
    /// </summary>
    private void RotateConcurrencyTokens()
    {
        ChangeTracker.DetectChanges();

        var touched = new HashSet<StockTransfer>(ReferenceEqualityComparer.Instance);

        // Tracked parents by id. A deleted child's navigation can be nulled by EF, so parents are
        // resolved through the FK against the tracked set rather than through the navigation.
        var transfersById = ChangeTracker.Entries<StockTransfer>()
            .Where(e => e.Entity.Id != 0)
            .ToDictionary(e => e.Entity.Id, e => e.Entity);

        var receiptsById = ChangeTracker.Entries<TransferReceipt>()
            .Where(e => e.Entity.Id != 0)
            .ToDictionary(e => e.Entity.Id, e => e.Entity);

        foreach (var entry in ChangeTracker.Entries<StockTransfer>()
            .Where(e => e.State == EntityState.Modified))
        {
            touched.Add(entry.Entity);
        }

        foreach (var entry in ChangeTracker.Entries<TransferLine>().Where(IsChanged))
        {
            var transfer = entry.Entity.Transfer
                ?? transfersById.GetValueOrDefault(entry.Entity.TransferId);

            if (transfer is not null)
                touched.Add(transfer);
        }

        foreach (var entry in ChangeTracker.Entries<TransferReceipt>().Where(IsChanged))
        {
            var transfer = entry.Entity.Transfer
                ?? transfersById.GetValueOrDefault(entry.Entity.TransferId);

            if (transfer is not null)
                touched.Add(transfer);
        }

        foreach (var entry in ChangeTracker.Entries<TransferReceiptLine>().Where(IsChanged))
        {
            var receipt = entry.Entity.Receipt
                ?? receiptsById.GetValueOrDefault(entry.Entity.ReceiptId);

            var transfer = receipt?.Transfer
                ?? (receipt is null ? null : transfersById.GetValueOrDefault(receipt.TransferId));

            if (transfer is not null)
                touched.Add(transfer);
        }

        foreach (var transfer in touched)
        {
            // A brand-new transfer has no stored token to protect yet.
            if (Entry(transfer).State is EntityState.Added or EntityState.Detached)
                continue;

            transfer.RotateConcurrencyToken();
        }
    }

    private static bool IsChanged(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry) =>
        entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;
}
