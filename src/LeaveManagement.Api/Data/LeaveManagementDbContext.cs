using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.Persistence.Context;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;

namespace StarterKit.LeaveManagement.Api.Data;

public class LeaveManagementDbContext(
    ICurrentUser currentUser,
    IDateTime clock,
    DbContextOptions<LeaveManagementDbContext> options) :
    BaseDbContext(options)
{
    public const string Schema = "leave";

    public virtual DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

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

        builder.Entity<LeaveRequest>(entity =>
        {
            entity.ToTable(name: "LeaveRequests");

            entity.HasIndex(x => new { x.EmployeeId, x.Status });

            entity.HasIndex(x => new { x.UserId, x.Status });

            entity.ConfigureAuditableEntity();

            entity.Property(x => x.UserId).HasMaxLength(450);

            entity.Property(x => x.EmployeeId).HasMaxLength(450);

            entity.Property(x => x.Reason).HasMaxLength(1000);

            entity.Property(x => x.ApprovalRequestId).HasMaxLength(450);

            // Value object over the existing StartDate/EndDate columns — no new columns, no
            // rename. Mapped via OwnsOne (owned entity, same table via table splitting), not
            // ComplexProperty: relational comparisons (<=, >=, etc.) on a ComplexProperty member do
            // not translate to SQL on this EF Core version, which OverlappingLeaveRequestsSpec's
            // overlap-check query needs. Same generated columns either way — confirmed via an empty
            // dotnet ef migrations add diff against MSSQL.
            entity.OwnsOne(
                x => x.Period,
                period =>
                {
                    period.Property(p => p.Start).HasColumnName("StartDate");

                    period.Property(p => p.End).HasColumnName("EndDate");
                });
        });
    }
}
