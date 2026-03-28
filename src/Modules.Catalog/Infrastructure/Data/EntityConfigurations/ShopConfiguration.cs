using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monolith.Catalog.Domain.Shops;

namespace Monolith.Catalog.Infrastructure.Data.EntityConfigurations;

internal class ShopConfiguration : IEntityTypeConfiguration<Shop>
{
    public void Configure(EntityTypeBuilder<Shop> entity)
    {
        entity.Property(p => p.Name).HasMaxLength(250);

        // Configure a relationship where the ActiveStatus is owned by (or part of) Entity.
        entity.OwnsOne(o => o.Status).Property(p => p.Value).HasColumnName("Status");
        entity.Navigation(emp => emp.Status).IsRequired();
    }
}