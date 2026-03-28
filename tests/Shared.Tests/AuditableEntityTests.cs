using Monolith;
using Xunit;

namespace Shared.Tests;

public class AuditableEntityTests
{
    [Fact]
    public void AuditableEntity_ShouldBeAbstract()
    {
        // Assert
        Assert.True(typeof(AuditableEntity).IsAbstract);
    }

    [Fact]
    public void AuditableEntity_Generic_ShouldBeAbstract()
    {
        // Assert
        Assert.True(typeof(AuditableEntity<>).IsAbstract);
    }
}