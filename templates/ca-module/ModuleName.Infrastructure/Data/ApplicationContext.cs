using StarterKit.Persistence.Context;

namespace StarterKit.Modules.ModuleName.Infrastructure.Data;

/// <summary>
/// EF Core context of the ModuleName module. Add <c>DbSet</c> properties and entity
/// configuration in <c>ConfigureModel</c> as the domain grows.
/// </summary>
public class ApplicationContext(DbContextOptions<ApplicationContext> options)
    : BaseDbContext(options),
    IApplicationContext
{
    public const string Schema = "ModuleName";

    protected override void ConfigureModel(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);
    }
}
