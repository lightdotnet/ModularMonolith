using StarterKit.Persistence.Extensions;

namespace StarterKit.Persistence.Context;

public abstract class BaseDbContext(DbContextOptions options) : DbContext(options)
{
    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureModel(modelBuilder);
        Database.FixSqliteDateTimeOffset(modelBuilder);
    }

    protected virtual void ConfigureModel(ModelBuilder builder) { }
}
