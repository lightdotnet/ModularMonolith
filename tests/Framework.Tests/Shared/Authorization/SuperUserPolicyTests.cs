using StarterKit.Shared.Authorization;
using Xunit;

namespace Framework.Tests.Shared.Authorization;

public class SuperUserPolicyTests
{
    [Fact]
    public void IsSuper_ShouldReturnTrue_ForTheSuperUserName()
    {
        // Act & Assert
        Assert.True(SuperUserPolicy.IsSuper(SuperUserPolicy.SuperUserName));
    }

    [Fact]
    public void IsSuper_ShouldBeCaseSensitive()
    {
        // Act & Assert
        Assert.False(SuperUserPolicy.IsSuper("Super"));
    }

    [Fact]
    public void IsSuper_ShouldReturnFalse_ForNullOrOtherNames()
    {
        // Act & Assert
        Assert.False(SuperUserPolicy.IsSuper(null));
        Assert.False(SuperUserPolicy.IsSuper("regular-user"));
    }
}
