using StarterKit.Shared.Entities;
using Xunit;

namespace Framework.Tests.Shared;

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
