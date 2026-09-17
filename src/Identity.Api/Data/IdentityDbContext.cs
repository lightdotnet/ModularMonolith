using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using StarterKit.Identity.Api.Entities;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;

namespace StarterKit.Identity.Api.Data;

public class IdentityDbContext(
    ICurrentUser currentUser,
    IDateTime clock,
    DbContextOptions<IdentityDbContext> options) :
    IdentityDbContext<User, Role, string, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>(options)
{
    public const string Schema = "identity";

    public virtual DbSet<UserSession> UserSessions => Set<UserSession>();

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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema(Schema);

        builder.BuildEntities();

        Database.FixSqliteDateTimeOffset(builder);
    }
}