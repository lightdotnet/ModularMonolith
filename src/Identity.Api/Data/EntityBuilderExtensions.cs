using StarterKit.Identity.Api.Entities;

namespace StarterKit.Identity.Api.Data;

internal static class EntityBuilderExtensions
{
    public static void BuildEntities(this ModelBuilder builder)
    {
        builder.Entity<Role>().ToTable(name: Tables.Roles);

        builder.Entity<RoleClaim>().ToTable(name: Tables.RoleClaims);

        builder.Entity<User>(entity =>
        {
            entity.ToTable(name: Tables.Users);

            entity.HasIndex(x => x.Created);

            // Configure a relationship where the ActiveStatus is owned by (or part of) User.
            entity.OwnsOne(o => o.Status).Property(p => p.Value).HasColumnName("Status");
            entity.Navigation(emp => emp.Status).IsRequired();

            // AuthProvider is a non-nullable enum; EF maps it to a non-nullable int column by default.
        });

        builder.Entity<UserRole>().ToTable(name: Tables.UserRoles);

        builder.Entity<UserLogin>().ToTable(name: Tables.UserLogins);

        builder.Entity<UserClaim>().ToTable(name: Tables.UserClaims);

        builder.Entity<UserToken>().ToTable(name: Tables.UserTokens);

        builder.Entity<UserSession>(e =>
        {
            e.ToTable(name: Tables.UserSessions);

            e.HasIndex(i => i.UserId);
        });
    }
}
