using Light.Mediator;
using StarterKit.Approval.Api.Domain.Approvals;
using StarterKit.Persistence.Context;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;

namespace StarterKit.Approval.Api.Data;

public class ApprovalDbContext(
    ICurrentUser currentUser,
    IDateTime clock,
    IPublisher publisher,
    DbContextOptions<ApprovalDbContext> options) :
    BaseDbContext(options)
{
    public const string Schema = "approval";

    public virtual DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();

    public virtual DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();

    public virtual DbSet<ApprovalDocumentType> ApprovalDocumentTypes => Set<ApprovalDocumentType>();

    public override int SaveChanges()
    {
        // The Approval write path is 100% async (ApprovalService only ever calls SaveChangesAsync),
        // so this override stays audit-only and does not dispatch domain events.
        this.AuditEntries(currentUser.UserId, clock.AuditTime, false);
        RotateConcurrencyTokens();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.AuditEntries(currentUser.UserId, clock.AuditTime, false);
        RotateConcurrencyTokens();

        var result = await base.SaveChangesAsync(cancellationToken);

        await publisher.DispatchDomainEvents(this);

        return result;
    }

    protected override void ConfigureModel(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);

        builder.Entity<ApprovalRequest>(entity =>
        {
            entity.ToTable(name: "ApprovalRequests");

            entity.HasIndex(x => new { x.RequestType, x.RequestId, x.Created });

            entity.HasIndex(x => x.RequesterUserId);

            entity.HasIndex(x => x.DocumentTypeId);

            entity.Property(x => x.ConcurrencyToken)
                .IsConcurrencyToken()
                .HasMaxLength(32)
                .IsRequired();

            entity.ConfigureAuditableEntity();

            entity.Property(x => x.RequestType).HasMaxLength(100);

            entity.Property(x => x.RequestId).HasMaxLength(450);

            entity.Property(x => x.RequesterUserId).HasMaxLength(450);

            entity.Property(x => x.RequesterEmployeeId).HasMaxLength(450);

            entity.Property(x => x.RequesterName).HasMaxLength(256);

            entity.Property(x => x.Title).HasMaxLength(250);

            entity.Property(x => x.Content).HasMaxLength(4000);

            entity.Property(x => x.DocumentTypeId).HasMaxLength(450);

            entity.HasOne(x => x.DocumentType)
                .WithMany()
                .HasForeignKey(x => x.DocumentTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Steps)
                .WithOne(x => x.ApprovalRequest)
                .HasForeignKey(x => x.ApprovalRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(x => x.Steps)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<ApprovalStep>(entity =>
        {
            entity.ToTable(name: "ApprovalSteps");

            entity.HasIndex(x => x.ApprovalRequestId);

            entity.HasIndex(x => x.ApproverUserId);

            entity.ConfigureAuditableEntity();

            entity.Property(x => x.ApprovalRequestId).HasMaxLength(450);

            entity.Property(x => x.ApproverUserId).HasMaxLength(450);

            entity.Property(x => x.ApproverEmployeeId).HasMaxLength(450);

            entity.Property(x => x.ApproverName).HasMaxLength(256);

            entity.Property(x => x.Comment).HasMaxLength(1000);
        });

        builder.Entity<ApprovalDocumentType>(entity =>
        {
            entity.ToTable(name: "ApprovalDocumentTypes");

            entity.HasIndex(x => x.Code).IsUnique();

            entity.ConfigureAuditableEntity();

            entity.Property(x => x.Name).HasMaxLength(200);

            entity.Property(x => x.Code).HasMaxLength(50);

            entity.Property(x => x.Description).HasMaxLength(1000);
        });
    }

    private void RotateConcurrencyTokens()
    {
        foreach (var entry in ChangeTracker.Entries<ApprovalRequest>()
            .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.RotateConcurrencyToken();
        }
    }
}
