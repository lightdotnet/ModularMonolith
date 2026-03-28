using Monolith.Catalog.Domain.Products;
using Monolith.Database;
using System.Reflection;

namespace Monolith.Catalog.Infrastructure.Data;

public class CatalogContext(
    ICurrentUser currentUser,
    IDateTime clock,
    DbContextOptions<CatalogContext> options) : BaseDbContext(options)
{
    public virtual DbSet<Category> Categories => Set<Category>();

    public virtual DbSet<Product> Products => Set<Product>();

    public virtual DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public virtual DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.AuditEntries(currentUser.UserId, clock.Now);
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
