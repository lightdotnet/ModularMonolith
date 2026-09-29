using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StarterKit.Persistence.Extensions;
using Xunit;

namespace Framework.Tests.Persistence;

public class SqliteDbContextExtensionsTests
{
    private sealed class SqliteEntity
    {
        public int Id { get; set; }
        public DateTimeOffset Occurred { get; set; }
        public DateTimeOffset? OccurredNullable { get; set; }
    }

    private sealed class SqliteTestDbContext(DbContextOptions<SqliteTestDbContext> options) : DbContext(options)
    {
        public DbSet<SqliteEntity> Entities => Set<SqliteEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Database.FixSqliteDateTimeOffset(modelBuilder);
        }
    }

    [Fact]
    public async Task FixSqliteDateTimeOffset_ShouldRoundTrip_TruncatedToWholeSeconds()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<SqliteTestDbContext>().UseSqlite(connection).Options;
        var moment = new DateTimeOffset(2024, 5, 1, 10, 30, 15, 123, TimeSpan.Zero);

        using (var context = new SqliteTestDbContext(options))
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
            context.Entities.Add(new SqliteEntity { Id = 1, Occurred = moment, OccurredNullable = moment });
            await context.SaveChangesAsync(cancellationToken);
        }

        // Act
        SqliteEntity reloaded;
        using (var context = new SqliteTestDbContext(options))
        {
            reloaded = await context.Entities.SingleAsync(e => e.Id == 1, cancellationToken);
        }

        // Assert
        var truncated = new DateTimeOffset(2024, 5, 1, 10, 30, 15, TimeSpan.Zero);
        Assert.Equal(truncated, reloaded.Occurred);
        Assert.Equal(truncated, reloaded.OccurredNullable);
    }

    [Fact]
    public async Task FixSqliteDateTimeOffset_ShouldRoundTripEpochValue_NotAsNull()
    {
        // Arrange: the nullable converter used to use 0 as both the "null" sentinel and a valid Unix
        // timestamp (epoch), so a genuinely-stored epoch value collided with null. Now it uses a
        // nullable long provider type, so there's no ambiguity between "no value" and "epoch".
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<SqliteTestDbContext>().UseSqlite(connection).Options;

        using (var context = new SqliteTestDbContext(options))
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
            context.Entities.Add(new SqliteEntity { Id = 2, Occurred = DateTimeOffset.UnixEpoch, OccurredNullable = DateTimeOffset.UnixEpoch });
            await context.SaveChangesAsync(cancellationToken);
        }

        // Act
        SqliteEntity reloaded;
        using (var context = new SqliteTestDbContext(options))
        {
            reloaded = await context.Entities.SingleAsync(e => e.Id == 2, cancellationToken);
        }

        // Assert
        Assert.Equal(DateTimeOffset.UnixEpoch, reloaded.Occurred);
        Assert.Equal(DateTimeOffset.UnixEpoch, reloaded.OccurredNullable);
    }

    [Fact]
    public async Task FixSqliteDateTimeOffset_ShouldRoundTripNull_AsNull()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<SqliteTestDbContext>().UseSqlite(connection).Options;

        using (var context = new SqliteTestDbContext(options))
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
            context.Entities.Add(new SqliteEntity { Id = 3, Occurred = DateTimeOffset.UtcNow, OccurredNullable = null });
            await context.SaveChangesAsync(cancellationToken);
        }

        // Act
        SqliteEntity reloaded;
        using (var context = new SqliteTestDbContext(options))
        {
            reloaded = await context.Entities.SingleAsync(e => e.Id == 3, cancellationToken);
        }

        // Assert
        Assert.Null(reloaded.OccurredNullable);
    }
}
