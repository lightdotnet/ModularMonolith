using StarterKit.Catalog.Api.Domain.Categories;
using StarterKit.Catalog.Api.Domain.Products;
using StarterKit.Persistence.Context;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;

namespace StarterKit.Catalog.Api.Data;

public class CatalogDbContext(
    ICurrentUser currentUser,
    IDateTime clock,
    DbContextOptions<CatalogDbContext> options) :
    BaseDbContext(options)
{
    public const string Schema = "catalog";

    public virtual DbSet<Category> Categories => Set<Category>();

    public virtual DbSet<Product> Products => Set<Product>();

    public override int SaveChanges()
    {
        this.AuditEntries(currentUser.UserId, clock.AuditTime, true);
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.AuditEntries(currentUser.UserId, clock.AuditTime, true);
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void ConfigureModel(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);

        builder.Entity<Category>(entity =>
        {
            entity.ToTable(name: "Categories");

            entity.HasIndex(x => new { x.ParentCategoryId, x.Name }).IsUnique();

            entity.HasIndex(x => x.ParentCategoryId);

            entity.ConfigureAuditableEntity();

            entity.Property(x => x.Name).HasMaxLength(200);

            entity.Property(x => x.ParentCategoryId).HasMaxLength(450);

            entity.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Product>(entity =>
        {
            entity.ToTable(name: "Products");

            // Filtered unique index, deliberately not scoped by Deleted: a soft-deleted product's
            // still-assigned SKU continues to occupy the constraint until explicitly cleared via
            // ClearSku/RemoveProductSkuCommand — reuse requires that explicit step, soft-delete
            // alone does not free it. Nulls (a cleared SKU) are excluded so any number of products
            // can share a cleared SKU.
            entity.HasIndex(x => x.Sku).IsUnique().HasFilter("[Sku] IS NOT NULL");

            entity.HasIndex(x => x.CategoryId);

            entity.ConfigureAuditableEntity<Product, long>();

            entity.Property(x => x.CategoryId).HasMaxLength(450);

            entity.Property(x => x.Name).HasMaxLength(200);

            entity.Property(x => x.Description).HasMaxLength(2000);

            // First active query filter in this repo — the precedent for the next soft-deletable
            // aggregate. GetProductById/ListProducts/CatalogPricingService need no code change: the
            // filter already hides soft-deleted products from every default read.
            entity.HasQueryFilter(x => x.Deleted == null);

            // Converted scalar column, not an owned type — keeps a plain unique index; owned-type
            // index syntax has no precedent in this repo. Uniqueness itself is belt-and-suspenders:
            // CreateProductCommandHandler pre-checks before insert, this index is the DB backstop.
            // Null-safe conversion: Sku became nullable once a product's SKU can be cleared.
            entity.Property(x => x.Sku)
                .HasConversion(sku => sku == null ? null : sku.Value, v => v == null ? null : new Sku(v))
                .HasMaxLength(Sku.MaxLength);

            // Table-split owned type (same row) — mutated in place via Money.Update rather than
            // reassigned, see Product.Reprice.
            entity.OwnsOne(
                x => x.Price,
                price =>
                {
                    price.Property(p => p.Amount).HasColumnName("PriceAmount").HasColumnType("decimal(18,2)");

                    price.Property(p => p.Currency).HasColumnName("PriceCurrency").HasMaxLength(3);
                });

            entity.Navigation(x => x.Price).IsRequired();

            // Table-split owned type (same row) — mutated in place via VatPercentage.Update rather
            // than reassigned, see Product.UpdateVatRate.
            entity.OwnsOne(
                x => x.VatRate,
                vat =>
                {
                    vat.Property(p => p.Value).HasColumnName("VatRate").HasColumnType("decimal(5,2)");
                });

            entity.Navigation(x => x.VatRate).IsRequired();

            // Owned collection into its own table — shadow int Id surrogate key as the sole PK
            // (not composite with the owner FK): Sqlite only auto-populates an INTEGER PRIMARY KEY
            // via its rowid-alias optimization when that column is the sole PK member, so a
            // composite (ProductId, Id) key leaves Id unpopulated on insert there. ProductId is a
            // plain indexed FK column instead. No JSON-column mapping: no precedent for that here,
            // and provider portability across InMemory/PostgreSQL/MSSQL/Sqlite is worse for a JSON
            // column.
            entity.OwnsMany(
                x => x.Images,
                image =>
                {
                    image.ToTable("ProductImages");

                    image.WithOwner().HasForeignKey("ProductId");

                    image.Property<int>("Id");

                    image.HasKey("Id");

                    image.HasIndex("ProductId");

                    image.Property(x => x.Url).HasMaxLength(2048);
                });

            entity.Navigation(x => x.Images).UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasOne<Category>()
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
