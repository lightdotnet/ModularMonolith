using StarterKit.Locations.Api.Domain.LocationTypes;
using StarterKit.Locations.Api.Domain.Locations;
using StarterKit.Persistence.Context;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;

namespace StarterKit.Locations.Api.Data;

public class LocationDbContext(
    ICurrentUser currentUser,
    IDateTime clock,
    DbContextOptions<LocationDbContext> options) :
    BaseDbContext(options)
{
    public const string Schema = "location";

    public virtual DbSet<Location> Locations => Set<Location>();

    public virtual DbSet<LocationType> LocationTypes => Set<LocationType>();

    public override int SaveChanges()
    {
        this.AuditEntries(currentUser.UserId, clock.AuditTime, false);
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.AuditEntries(currentUser.UserId, clock.AuditTime, false);
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void ConfigureModel(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);

        builder.Entity<Location>(entity =>
        {
            entity.ToTable(name: "Locations");

            entity.HasIndex(x => x.Code).IsUnique();

            entity.HasIndex(x => x.ParentLocationId);

            entity.HasIndex(x => x.LocationTypeId);

            entity.ConfigureAuditableEntity();

            entity.Property(x => x.Name).HasMaxLength(200);

            entity.Property(x => x.Code).HasMaxLength(50);

            entity.Property(x => x.LocationTypeId).HasMaxLength(450);

            entity.Property(x => x.ParentLocationId).HasMaxLength(450);

            entity.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Type)
                .WithMany()
                .HasForeignKey(x => x.LocationTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LocationType>(entity =>
        {
            entity.ToTable(name: "LocationTypes");

            entity.HasIndex(x => x.AllowedParentTypeId);

            entity.ConfigureAuditableEntity();

            entity.Property(x => x.Id).ValueGeneratedNever();

            entity.Property(x => x.Name).HasMaxLength(200);

            entity.Property(x => x.AllowedParentTypeId).HasMaxLength(450);

            entity.HasOne<LocationType>()
                .WithMany()
                .HasForeignKey(x => x.AllowedParentTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
