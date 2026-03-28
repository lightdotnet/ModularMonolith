using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Monolith.Database;

namespace Monolith.Identity.Data;

public class IdentityDbContext(
    ICurrentUser currentUser,
    IDateTime clock,
    DbContextOptions<IdentityDbContext> options) :
    IdentityDbContext<User, Role, string, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>(options)
{
    public virtual DbSet<JwtToken> JwtTokens => Set<JwtToken>();

    public virtual DbSet<Notification> Notifications => Set<Notification>();

    public override int SaveChanges()
    {
        this.AuditEntries(currentUser.UserId, clock.Now, false);
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.AuditEntries(currentUser.UserId, clock.Now, false);
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Role>().ToTable(name: Tables.Roles, Schemas.Identity);

        builder.Entity<RoleClaim>().ToTable(name: Tables.RoleClaims, Schemas.Identity);

        builder.Entity<User>(entity =>
        {
            entity.ToTable(name: Tables.Users, Schemas.Identity);

            // Configure a relationship where the ActiveStatus is owned by (or part of) User.
            entity.OwnsOne(o => o.Status).Property(p => p.Value).HasColumnName("Status");
            entity.Navigation(emp => emp.Status).IsRequired();
        });

        builder.Entity<UserRole>().ToTable(name: Tables.UserRoles, Schemas.Identity);

        builder.Entity<UserLogin>().ToTable(name: Tables.UserLogins, Schemas.Identity);

        builder.Entity<UserClaim>().ToTable(name: Tables.UserClaims, Schemas.Identity);

        builder.Entity<UserToken>().ToTable(name: Tables.UserTokens, Schemas.Identity);

        builder.Entity<JwtToken>(e =>
        {
            e.ToTable(name: Tables.JwtTokens, Schemas.Identity);

            e.HasIndex(i => i.UserId);
        });

        builder.Entity<Notification>().ToTable(name: "Notifications", Schemas.System);

        Database.FixDateTimeOffsetSqlite(builder);
    }
}
