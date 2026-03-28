using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monolith.Catalog.Domain.Products;

namespace Monolith.Catalog.Infrastructure.Data.EntityConfigurations;

internal class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.Property(p => p.Code).HasMaxLength(100);

        entity.Property(p => p.Name).HasMaxLength(250);

        entity.Property(p => p.Description).HasMaxLength(1000);

        entity.Property(p => p.CategoryId).HasMaxLength(450);

        entity.Property(p => p.ShopId).HasMaxLength(450);

        entity.Property(p => p.MainImage).HasMaxLength(1000);

        entity.HasOne(d => d.Category)
            .WithMany(p => p.Products)
            .HasForeignKey(fk => fk.CategoryId) // key for virtual map from n
            .HasPrincipalKey(pk => pk.Id)
            .OnDelete(DeleteBehavior.Restrict); // key for virtual map from 1
    }
}

internal class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> entity)
    {
        entity.Property(p => p.ProductId).HasMaxLength(450);

        entity.Property(p => p.ImageUrl).HasMaxLength(1000);

        entity.HasOne(d => d.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(fk => fk.ProductId) // key for virtual map from n
            .HasPrincipalKey(pk => pk.Id)
            .OnDelete(DeleteBehavior.Restrict); // key for virtual map from 1
    }
}

internal class ProductPriceConfiguration : IEntityTypeConfiguration<ProductPrice>
{
    public void Configure(EntityTypeBuilder<ProductPrice> entity)
    {
        entity.Property(p => p.ProductId).HasMaxLength(450);

        entity.Property(p => p.Type).HasConversion<string>();
    }
}