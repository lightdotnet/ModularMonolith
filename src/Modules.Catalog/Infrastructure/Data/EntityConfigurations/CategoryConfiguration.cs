using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Monolith.Catalog.Infrastructure.Data.EntityConfigurations;

internal class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> entity)
    {
        entity.Property(p => p.SubOfId).HasMaxLength(450);
    }
}