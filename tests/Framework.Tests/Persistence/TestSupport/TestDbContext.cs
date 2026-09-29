using Microsoft.EntityFrameworkCore;

namespace Framework.Tests.Persistence.TestSupport;

public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestAggregate> Aggregates => Set<TestAggregate>();

    public DbSet<TestCreationOnlyEntity> CreationOnlyEntities => Set<TestCreationOnlyEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestAggregate>().OwnsOne(a => a.Note);
    }

    public static TestDbContext CreateInMemory(string? databaseName = null) =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options);
}
