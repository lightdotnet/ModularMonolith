using Light.Mediator;
using Microsoft.Extensions.Logging;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Orders.Api.Domain.Payments;
using StarterKit.Persistence.Context;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;

namespace StarterKit.Orders.Api.Data;

public class OrdersDbContext(
    ICurrentUser currentUser,
    IDateTime clock,
    IPublisher publisher,
    ILogger<OrdersDbContext> logger,
    DbContextOptions<OrdersDbContext> options) :
    BaseDbContext(options)
{
    public const string Schema = "orders";

    public virtual DbSet<Order> Orders => Set<Order>();

    public virtual DbSet<OrderLine> OrderLines => Set<OrderLine>();

    public virtual DbSet<OrderFee> OrderFees => Set<OrderFee>();

    public virtual DbSet<Payment> Payments => Set<Payment>();

    public override int SaveChanges()
    {
        // The Orders write path is 100% async (every handler only ever calls SaveChangesAsync), so
        // this override stays audit-only and does not dispatch domain events — mirrors
        // ApprovalDbContext.
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
                "One or more Orders domain-event handlers threw after SaveChangesAsync; the write is already committed.");
        }

        return result;
    }

    protected override void ConfigureModel(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);

        builder.Entity<Order>(entity =>
        {
            entity.ToTable(name: "Orders");

            entity.HasIndex(x => x.LocationId);

            // Converted scalar column, not an owned type — same reasoning as Catalog's Sku/this
            // module's own OrderLine.Sku/ProductId snapshots.
            entity.HasIndex(x => x.OrderCode).IsUnique();

            entity.Property(x => x.ConcurrencyToken)
                .IsConcurrencyToken()
                .HasMaxLength(32)
                .IsRequired();

            entity.ConfigureAuditableEntity<Order, long>();

            entity.Property(x => x.LocationId).HasMaxLength(450);

            entity.Property(x => x.MemberId).HasMaxLength(450);

            entity.Property(x => x.CancelledReason).HasMaxLength(1000);

            entity.Property(x => x.OrderCode)
                .HasConversion(orderCode => orderCode.Value, v => new OrderCode(v))
                .HasMaxLength(OrderCode.MaxLength);

            entity.Property(x => x.ExternalReferenceCode).HasMaxLength(50);

            // Table-split owned type (same row) — always present, mutated in place via Money.Update
            // rather than reassigned, see Order.ReconcilePaymentStatus.
            entity.OwnsOne(
                x => x.AmountPaid,
                money =>
                {
                    money.Property(p => p.Amount).HasColumnName("AmountPaidAmount").HasColumnType("decimal(18,2)");

                    money.Property(p => p.Currency).HasColumnName("AmountPaidCurrency").HasMaxLength(3);
                });

            entity.Navigation(x => x.AmountPaid).IsRequired();

            // Optional owned type — unlike Price/VatRate above, an order may carry no discount at
            // all, so the navigation is simply left unconfigured as required. See OrderDiscount's
            // own doc comment for why an already-present discount is mutated in place rather than
            // reassigned.
            entity.OwnsOne(
                x => x.Discount,
                discount =>
                {
                    discount.Property(p => p.Kind).HasColumnName("DiscountKind");

                    discount.Property(p => p.Value).HasColumnName("DiscountValue").HasColumnType("decimal(18,2)");
                });

            // OrderLine/OrderFee have their own identity (own Id, individually removable) — a
            // normal HasMany/WithOne relationship into their own tables, not an owned collection.
            // Mirrors ApprovalRequest.Steps/ApprovalStep.
            entity.HasMany(x => x.Lines)
                .WithOne(x => x.Order)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(x => x.Fees)
                .WithOne(x => x.Order)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(x => x.Fees).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<OrderLine>(entity =>
        {
            entity.ToTable(name: "OrderLines");

            entity.HasIndex(x => x.OrderId);

            entity.ConfigureAuditableEntity<OrderLine, long>();

            entity.Property(x => x.ProductName).HasMaxLength(200);

            entity.Property(x => x.Sku).HasMaxLength(100);

            // Plain denormalized snapshot of the parent Order.OrderCode.Value — no FK, no index,
            // same treatment as Sku/ProductId above.
            entity.Property(x => x.OrderCode).HasMaxLength(OrderCode.MaxLength);

            entity.OwnsOne(
                x => x.UnitPrice,
                price =>
                {
                    price.Property(p => p.Amount).HasColumnName("UnitPriceAmount").HasColumnType("decimal(18,2)");

                    price.Property(p => p.Currency).HasColumnName("UnitPriceCurrency").HasMaxLength(3);
                });

            entity.Navigation(x => x.UnitPrice).IsRequired();

            entity.OwnsOne(
                x => x.VatRate,
                vat =>
                {
                    vat.Property(p => p.Value).HasColumnName("VatRate").HasColumnType("decimal(5,2)");
                });

            entity.Navigation(x => x.VatRate).IsRequired();

            // Optional owned type — see OrderLine.SetRequestedSalePrice's own doc comment for why an
            // already-present override is mutated in place rather than reassigned.
            entity.OwnsOne(
                x => x.RequestedSalePrice,
                price =>
                {
                    price.Property(p => p.Amount)
                        .HasColumnName("RequestedSalePriceAmount")
                        .HasColumnType("decimal(18,2)");

                    price.Property(p => p.Currency).HasColumnName("RequestedSalePriceCurrency").HasMaxLength(3);
                });
        });

        builder.Entity<OrderFee>(entity =>
        {
            entity.ToTable(name: "OrderFees");

            entity.HasIndex(x => x.OrderId);

            entity.ConfigureAuditableEntity<OrderFee, long>();

            entity.Property(x => x.Name).HasMaxLength(200);

            // Plain denormalized snapshot of the parent Order.OrderCode.Value — no FK, no index.
            entity.Property(x => x.OrderCode).HasMaxLength(OrderCode.MaxLength);

            entity.OwnsOne(
                x => x.Amount,
                money =>
                {
                    money.Property(p => p.Amount).HasColumnName("AmountAmount").HasColumnType("decimal(18,2)");

                    money.Property(p => p.Currency).HasColumnName("AmountCurrency").HasMaxLength(3);
                });

            entity.Navigation(x => x.Amount).IsRequired();
        });

        builder.Entity<Payment>(entity =>
        {
            entity.ToTable(name: "Payments");

            entity.HasIndex(x => x.OrderId);

            entity.ConfigureAuditableEntity<Payment, long>();

            entity.Property(x => x.Reference).HasMaxLength(200);

            entity.Property(x => x.RecordedByUserId).HasMaxLength(450);

            entity.Property(x => x.VoidReason).HasMaxLength(1000);

            // Plain denormalized snapshot of the parent Order.OrderCode.Value — no FK, no index.
            entity.Property(x => x.OrderCode).HasMaxLength(OrderCode.MaxLength);

            entity.OwnsOne(
                x => x.Amount,
                money =>
                {
                    money.Property(p => p.Amount).HasColumnName("AmountAmount").HasColumnType("decimal(18,2)");

                    money.Property(p => p.Currency).HasColumnName("AmountCurrency").HasMaxLength(3);
                });

            entity.Navigation(x => x.Amount).IsRequired();
        });
    }

    private void RotateConcurrencyTokens()
    {
        foreach (var entry in ChangeTracker.Entries<Order>()
            .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.RotateConcurrencyToken();
        }
    }
}
