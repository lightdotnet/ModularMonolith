using Framework.Tests.Persistence.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StarterKit.Persistence;
using Xunit;

namespace Framework.Tests.Persistence;

public class DbContextExtensionsTests
{
    private static IConfiguration BuildConfiguration(string? dbProvider)
    {
        var builder = new ConfigurationBuilder();
        if (dbProvider is not null)
        {
            builder.AddInMemoryCollection(new Dictionary<string, string?> { ["DbProvider"] = dbProvider });
        }
        return builder.Build();
    }

    [Fact]
    public void GetDbProvider_ShouldReturnConfiguredProvider()
    {
        // Arrange
        var configuration = BuildConfiguration("PostgreSQL");

        // Act & Assert
        Assert.Equal(DbProvider.PostgreSQL, configuration.GetDbProvider());
    }

    [Fact]
    public void GetDbProvider_ShouldDefaultToInMemory_WhenKeyIsAbsent()
    {
        // Arrange
        var configuration = BuildConfiguration(null);

        // Act & Assert
        Assert.Equal(DbProvider.InMemory, configuration.GetDbProvider());
    }

    [Theory]
    [InlineData(DbProvider.PostgreSQL, "Host=localhost;Database=test;Username=test;Password=test", "Npgsql.EntityFrameworkCore.PostgreSQL")]
    [InlineData(DbProvider.MSSQL, "Server=localhost;Database=test;Trusted_Connection=True;", "Microsoft.EntityFrameworkCore.SqlServer")]
    [InlineData(DbProvider.Sqlite, "Data Source=test.db", "Microsoft.EntityFrameworkCore.Sqlite")]
    public void ConfigureDatabase_ShouldConfigureMatchingProvider(DbProvider provider, string connectionString, string expectedProviderName)
    {
        // Arrange
        var builder = new DbContextOptionsBuilder<TestDbContext>();

        // Act
        builder.ConfigureDatabase(provider, connectionString);
        using var context = new TestDbContext(builder.Options);

        // Assert
        Assert.Equal(expectedProviderName, context.Database.ProviderName);
    }

    [Fact]
    public void ConfigureDatabase_ShouldThrow_ForUnsupportedProvider()
    {
        // Arrange
        var builder = new DbContextOptionsBuilder<TestDbContext>();
        var unsupportedProvider = (DbProvider)99;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.ConfigureDatabase(unsupportedProvider, "cs"));
    }
}
