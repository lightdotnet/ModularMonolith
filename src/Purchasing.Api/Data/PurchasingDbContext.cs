using Light.Mediator;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using StarterKit.Persistence.Context;
using StarterKit.Persistence.Extensions;
using StarterKit.Purchasing.Api.Domain.GoodsReceipts;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using StarterKit.Purchasing.Api.Domain.PurchaseReturns;
using StarterKit.Purchasing.Api.Domain.Suppliers;
using StarterKit.Shared;

namespace StarterKit.Purchasing.Api.Data;

public class PurchasingDbContext(
    ICurrentUser currentUser,
    IDateTime clock,
    IPublisher publisher,
    ILogger<PurchasingDbContext> logger,
    DbContextOptions<PurchasingDbContext> options) :
    BaseDbContext(options)
{
    public const string Schema = "purchasing";

    public virtual DbSet<Supplier> Suppliers => Set<Supplier>();

    public virtual DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    public virtual DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

    public virtual DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();

    public virtual DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();

    public virtual DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();

    public virtual DbSet<PurchaseReturnLine> PurchaseReturnLines => Set<PurchaseReturnLine>();

    public override int SaveChanges()
    {
        // The Purchasing write path is 100% async (every handler only ever calls SaveChangesAsync), so
        // this override stays audit-only and does not dispatch domain events — mirrors TransfersDbContext.
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
                "One or more Purchasing domain-event handlers threw after SaveChangesAsync; the write is already committed.");
        }

        return result;
    }

    protected override void ConfigureModel(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);

        builder.Entity<Supplier>(entity =>
        {
            entity.ToTable(name: "Suppliers");

            entity.HasIndex(x => x.Code).IsUnique();

            entity.HasIndex(x => new { x.Status, x.Name });

            entity.ConfigureAuditableEntity<Supplier, long>();

            entity.Property(x => x.Code).HasMaxLength(50);

            entity.Property(x => x.Name).HasMaxLength(200);

            entity.Property(x => x.ContactName).HasMaxLength(200);

            entity.Property(x => x.Phone).HasMaxLength(50);

            entity.Property(x => x.Email).HasMaxLength(256);

            entity.Property(x => x.Address).HasMaxLength(500);

            entity.Property(x => x.PaymentTerms).HasMaxLength(500);

            entity.Ignore(x => x.IsActive);
        });

        builder.Entity<PurchaseOrder>(entity =>
        {
            entity.ToTable(name: "PurchaseOrders");

            entity.HasIndex(x => x.PONumber).IsUnique();

            entity.HasIndex(x => x.SupplierId);

            entity.HasIndex(x => x.LocationId);

            // Serves the list filters and the approval reconciliation sweep (status = PendingApproval).
            entity.HasIndex(x => new { x.Status, x.Created });

            entity.Property(x => x.ConcurrencyToken)
                .IsConcurrencyToken()
                .HasMaxLength(32)
                .IsRequired();

            entity.ConfigureAuditableEntity<PurchaseOrder, long>();

            // Converted scalar column, not an owned type — same reasoning as Orders' OrderCode.
            entity.Property(x => x.PONumber)
                .HasConversion(number => number.Value, v => new PurchaseOrderNumber(v))
                .HasMaxLength(PurchaseOrderNumber.MaxLength);

            entity.Property(x => x.SupplierName).HasMaxLength(200);

            entity.Property(x => x.LocationId).HasMaxLength(450);

            entity.Property(x => x.LocationName).HasMaxLength(200);

            entity.Property(x => x.RequesterUserId).HasMaxLength(450);

            entity.Property(x => x.RequesterEmployeeId).HasMaxLength(450);

            entity.Property(x => x.ApproverEmployeeId).HasMaxLength(450);

            entity.Property(x => x.ApproverName).HasMaxLength(200);

            entity.Property(x => x.ApprovalRequestId).HasMaxLength(450);

            entity.Property(x => x.Note).HasMaxLength(1000);

            entity.Property(x => x.ClosedReason).HasMaxLength(1000);

            entity.Property(x => x.ClosedBy).HasMaxLength(450);

            entity.Property(x => x.CancelledBy).HasMaxLength(450);

            entity.Property(x => x.CancelledReason).HasMaxLength(1000);

            entity.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Lines)
                .WithOne(x => x.PurchaseOrder)
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(x => x.TotalAmount);

            entity.Ignore(x => x.TotalOrderedQuantity);

            entity.Ignore(x => x.TotalReceivedQuantity);

            entity.Ignore(x => x.TotalOutstandingQuantity);
        });

        builder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.ToTable(name: "PurchaseOrderLines");

            entity.HasIndex(x => x.PurchaseOrderId);

            entity.HasIndex(x => x.ProductId);

            entity.ConfigureAuditableEntity<PurchaseOrderLine, long>();

            entity.Property(x => x.ProductName).HasMaxLength(200);

            entity.Property(x => x.Sku).HasMaxLength(100);

            entity.Property(x => x.UnitCostAmount)
                .HasColumnName("UnitCost")
                .HasPrecision(19, 4);

            entity.Ignore(x => x.UnitCost);

            entity.Ignore(x => x.LineTotal);

            entity.Ignore(x => x.OutstandingQuantity);
        });

        builder.Entity<GoodsReceipt>(entity =>
        {
            entity.ToTable(name: "GoodsReceipts");

            entity.HasIndex(x => x.ReceiptNumber).IsUnique();

            // A delivery note reference is unique per purchase order, but only when one was given.
            // A voided receipt never counted, so its reference can be submitted again.
            entity.HasIndex(x => new { x.PurchaseOrderId, x.DeliveryNoteRef })
                .IsUnique()
                .HasProviderFilter(
                    Database,
                    "[DeliveryNoteRef] IS NOT NULL AND [Status] <> 2",
                    "\"DeliveryNoteRef\" IS NOT NULL AND \"Status\" <> 2");

            entity.HasIndex(x => x.SupplierId);

            entity.HasIndex(x => x.LocationId);

            // Supports the posting reconciliation sweep, which only looks at receipts stuck in Posting.
            entity.HasIndex(x => new { x.Status, x.Created });

            entity.Property(x => x.ConcurrencyToken)
                .IsConcurrencyToken()
                .HasMaxLength(32)
                .IsRequired();

            entity.ConfigureAuditableEntity<GoodsReceipt, long>();

            entity.Property(x => x.ReceiptNumber)
                .HasConversion(number => number.Value, v => new GoodsReceiptNumber(v))
                .HasMaxLength(GoodsReceiptNumber.MaxLength);

            entity.Property(x => x.SupplierName).HasMaxLength(200);

            entity.Property(x => x.LocationId).HasMaxLength(450);

            entity.Property(x => x.LocationName).HasMaxLength(200);

            entity.Property(x => x.DeliveryNoteRef).HasMaxLength(100);

            entity.Property(x => x.VoidReason).HasMaxLength(1000);

            entity.Property(x => x.ReceivedByUserId).HasMaxLength(450);

            entity.HasOne(x => x.PurchaseOrder)
                .WithMany()
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Lines)
                .WithOne(x => x.GoodsReceipt)
                .HasForeignKey(x => x.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(x => x.TotalQuantity);

            entity.Ignore(x => x.TotalCostBase);
        });

        builder.Entity<GoodsReceiptLine>(entity =>
        {
            entity.ToTable(name: "GoodsReceiptLines");

            entity.HasIndex(x => x.GoodsReceiptId);

            // Supports the in-flight quantity lookup when a further delivery is received.
            entity.HasIndex(x => x.PurchaseOrderLineId);

            entity.ConfigureAuditableEntity<GoodsReceiptLine, long>();

            entity.Property(x => x.ProductName).HasMaxLength(200);

            entity.Property(x => x.Sku).HasMaxLength(100);

            entity.Property(x => x.UnitCostBase).HasPrecision(19, 4);

            entity.Ignore(x => x.LineTotalBase);
        });

        builder.Entity<PurchaseReturn>(entity =>
        {
            entity.ToTable(name: "PurchaseReturns");

            entity.HasIndex(x => x.ReturnNumber).IsUnique();

            entity.HasIndex(x => x.GoodsReceiptId);

            entity.HasIndex(x => x.SupplierId);

            entity.HasIndex(x => x.LocationId);

            entity.HasIndex(x => new { x.Status, x.Created });

            // Supports the posting reconciliation sweep, which only looks at returns stuck in Posting.
            entity.HasIndex(x => x.PostingStartedAt)
                .HasProviderFilter(
                    Database,
                    "[PostingStartedAt] IS NOT NULL",
                    "\"PostingStartedAt\" IS NOT NULL");

            entity.Property(x => x.ConcurrencyToken)
                .IsConcurrencyToken()
                .HasMaxLength(32)
                .IsRequired();

            entity.ConfigureAuditableEntity<PurchaseReturn, long>();

            entity.Property(x => x.ReturnNumber)
                .HasConversion(number => number.Value, v => new PurchaseReturnNumber(v))
                .HasMaxLength(PurchaseReturnNumber.MaxLength);

            entity.Property(x => x.SupplierName).HasMaxLength(200);

            entity.Property(x => x.LocationId).HasMaxLength(450);

            entity.Property(x => x.LocationName).HasMaxLength(200);

            entity.Property(x => x.Note).HasMaxLength(1000);

            entity.Property(x => x.ExpectedCreditBase).HasPrecision(19, 4);

            entity.Property(x => x.CreditNoteNumber).HasMaxLength(100);

            entity.Property(x => x.PostedBy).HasMaxLength(450);

            entity.Property(x => x.CreditedBy).HasMaxLength(450);

            entity.Property(x => x.CancelledBy).HasMaxLength(450);

            entity.Property(x => x.CreditAmountBase).HasPrecision(19, 4);

            entity.Property(x => x.CancelledReason).HasMaxLength(1000);

            entity.HasOne(x => x.GoodsReceipt)
                .WithMany()
                .HasForeignKey(x => x.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Lines)
                .WithOne(x => x.PurchaseReturn)
                .HasForeignKey(x => x.PurchaseReturnId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(x => x.TotalQuantity);

            entity.Ignore(x => x.CostRemovedBase);
        });

        builder.Entity<PurchaseReturnLine>(entity =>
        {
            entity.ToTable(name: "PurchaseReturnLines");

            entity.HasIndex(x => x.PurchaseReturnId);

            // Supports the already-returned quantity lookup per receipt line.
            entity.HasIndex(x => x.GoodsReceiptLineId);

            entity.ConfigureAuditableEntity<PurchaseReturnLine, long>();

            entity.Property(x => x.ProductName).HasMaxLength(200);

            entity.Property(x => x.Sku).HasMaxLength(100);

            entity.Property(x => x.ReceiptUnitCostBase).HasPrecision(19, 4);

            entity.Property(x => x.CostRemovedBase).HasPrecision(19, 4);
        });
    }

    /// <summary>
    /// Rotates the concurrency token of every aggregate root that is itself modified <em>or</em> whose
    /// children (or a dependent document) changed:
    /// <list type="bullet">
    /// <item><description>a purchase order — when it or its lines change, or a goods receipt is recorded against it, so two racing receipts collide before either posts stock;</description></item>
    /// <item><description>a goods receipt — when it or a purchase return against it changes, so two racing returns cannot both pass the over-return guard;</description></item>
    /// <item><description>a purchase return — when it or its lines change, so the original request and the reconciliation sweep cannot both finish it.</description></item>
    /// </list>
    /// </summary>
    private void RotateConcurrencyTokens()
    {
        ChangeTracker.DetectChanges();

        var orders = new HashSet<PurchaseOrder>(ReferenceEqualityComparer.Instance);
        var receipts = new HashSet<GoodsReceipt>(ReferenceEqualityComparer.Instance);
        var returns = new HashSet<PurchaseReturn>(ReferenceEqualityComparer.Instance);

        // Tracked parents by id. A deleted child's navigation can be nulled by EF, so parents are
        // resolved through the FK against the tracked set rather than through the navigation.
        var ordersById = ChangeTracker.Entries<PurchaseOrder>()
            .Where(e => e.Entity.Id != 0)
            .ToDictionary(e => e.Entity.Id, e => e.Entity);

        var receiptsById = ChangeTracker.Entries<GoodsReceipt>()
            .Where(e => e.Entity.Id != 0)
            .ToDictionary(e => e.Entity.Id, e => e.Entity);

        var returnsById = ChangeTracker.Entries<PurchaseReturn>()
            .Where(e => e.Entity.Id != 0)
            .ToDictionary(e => e.Entity.Id, e => e.Entity);

        foreach (var entry in ChangeTracker.Entries<PurchaseOrder>().Where(e => e.State == EntityState.Modified))
            orders.Add(entry.Entity);

        foreach (var entry in ChangeTracker.Entries<PurchaseOrderLine>().Where(IsChanged))
        {
            var order = entry.Entity.PurchaseOrder
                ?? ordersById.GetValueOrDefault(entry.Entity.PurchaseOrderId);

            if (order is not null)
                orders.Add(order);
        }

        // A new receipt only inserts rows; touching the order is what fences a concurrent receive.
        foreach (var entry in ChangeTracker.Entries<GoodsReceipt>().Where(e => e.State == EntityState.Added))
        {
            var order = entry.Entity.PurchaseOrder
                ?? ordersById.GetValueOrDefault(entry.Entity.PurchaseOrderId);

            if (order is not null)
                orders.Add(order);
        }

        foreach (var entry in ChangeTracker.Entries<GoodsReceipt>().Where(e => e.State == EntityState.Modified))
            receipts.Add(entry.Entity);

        foreach (var entry in ChangeTracker.Entries<PurchaseReturn>().Where(IsChanged))
        {
            returns.Add(entry.Entity);

            var receipt = entry.Entity.GoodsReceipt
                ?? receiptsById.GetValueOrDefault(entry.Entity.GoodsReceiptId);

            if (receipt is not null)
                receipts.Add(receipt);
        }

        foreach (var entry in ChangeTracker.Entries<PurchaseReturnLine>().Where(IsChanged))
        {
            var purchaseReturn = entry.Entity.PurchaseReturn
                ?? returnsById.GetValueOrDefault(entry.Entity.PurchaseReturnId);

            if (purchaseReturn is null)
                continue;

            returns.Add(purchaseReturn);

            var receipt = purchaseReturn.GoodsReceipt
                ?? receiptsById.GetValueOrDefault(purchaseReturn.GoodsReceiptId);

            if (receipt is not null)
                receipts.Add(receipt);
        }

        foreach (var order in orders)
        {
            // A brand-new aggregate has no stored token to protect yet.
            if (Entry(order).State is EntityState.Added or EntityState.Detached)
                continue;

            order.RotateConcurrencyToken();
        }

        foreach (var receipt in receipts)
        {
            if (Entry(receipt).State is EntityState.Added or EntityState.Detached)
                continue;

            receipt.RotateConcurrencyToken();
        }

        foreach (var purchaseReturn in returns)
        {
            if (Entry(purchaseReturn).State is EntityState.Added or EntityState.Detached)
                continue;

            purchaseReturn.RotateConcurrencyToken();
        }
    }

    private static bool IsChanged(EntityEntry entry) =>
        entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;
}
