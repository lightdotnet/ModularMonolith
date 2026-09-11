using StarterKit.Organization.Api.Domain.Companies;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Api.Domain.OrgUnits;
using StarterKit.Persistence.Context;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;

namespace StarterKit.Organization.Api.Data;

public class OrganizationDbContext(
    ICurrentUser currentUser,
    IDateTime clock,
    DbContextOptions<OrganizationDbContext> options) :
    BaseDbContext(options)
{
    public const string Schema = "organization";

    public virtual DbSet<Company> Companies => Set<Company>();

    public virtual DbSet<OrgUnit> OrgUnits => Set<OrgUnit>();

    public virtual DbSet<EmployeeLevel> EmployeeLevels => Set<EmployeeLevel>();

    public virtual DbSet<Employee> Employees => Set<Employee>();

    public virtual DbSet<EmployeeOrgUnitMembership> EmployeeOrgUnitMemberships => Set<EmployeeOrgUnitMembership>();

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

        builder.Entity<Company>(entity =>
        {
            entity.ToTable(name: "Companies");

            entity.HasIndex(x => x.Code).IsUnique();

            entity.ConfigureAuditableEntity();

            entity.Property(x => x.Name).HasMaxLength(200);

            entity.Property(x => x.Code).HasMaxLength(50);

            entity.Property(x => x.TaxCode).HasMaxLength(50);

            entity.Property(x => x.Email).HasMaxLength(200);

            entity.Property(x => x.Phone).HasMaxLength(50);
        });

        builder.Entity<OrgUnit>(entity =>
        {
            entity.ToTable(name: "OrgUnits");

            entity.HasIndex(x => new { x.CompanyId, x.ParentId });

            entity.ConfigureAuditableEntity();

            entity.Property(x => x.Name).HasMaxLength(200);

            entity.Property(x => x.Code).HasMaxLength(50);

            entity.Property(x => x.CompanyId).HasMaxLength(450);

            entity.Property(x => x.ParentId).HasMaxLength(450);

            entity.Property(x => x.ManagerEmployeeId).HasMaxLength(450);

            entity.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EmployeeLevel>(entity =>
        {
            entity.ToTable(name: "EmployeeLevels");

            entity.HasIndex(x => x.CompanyId);

            entity.ConfigureAuditableEntity();

            entity.Property(x => x.Name).HasMaxLength(200);

            entity.Property(x => x.Code).HasMaxLength(50);

            entity.Property(x => x.CompanyId).HasMaxLength(450);

            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Employee>(entity =>
        {
            entity.ToTable(name: "Employees");

            entity.HasIndex(x => new { x.CompanyId, x.EmployeeCode }).IsUnique();

            entity.HasIndex(x => x.UserId).IsUnique();

            entity.ConfigureAuditableEntity();

            entity.Property(x => x.CompanyId).HasMaxLength(450);

            entity.Property(x => x.UserId).HasMaxLength(450);

            entity.Property(x => x.EmployeeCode).HasMaxLength(50);

            entity.Property(x => x.FirstName).HasMaxLength(100);

            entity.Property(x => x.LastName).HasMaxLength(100);

            entity.Property(x => x.Email).HasMaxLength(200);

            entity.Property(x => x.PhoneNumber).HasMaxLength(50);

            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EmployeeOrgUnitMembership>(entity =>
        {
            entity.ToTable(name: "EmployeeOrgUnitMemberships");

            entity.HasIndex(x => x.EmployeeId);

            entity.HasIndex(x => x.OrgUnitId);

            entity.ConfigureAuditableEntity();

            entity.Property(x => x.EmployeeId).HasMaxLength(450);

            entity.Property(x => x.OrgUnitId).HasMaxLength(450);

            entity.Property(x => x.LevelId).HasMaxLength(450);

            entity.HasOne(x => x.Employee)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.OrgUnit)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.OrgUnitId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Level)
                .WithMany()
                .HasForeignKey(x => x.LevelId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
