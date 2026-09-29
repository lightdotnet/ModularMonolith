using StarterKit.Shared.Constants;
using StarterKit.Shared.Utilities;
using Xunit;

namespace Framework.Tests.Shared.Utilities;

public class ReflectionHelperTests
{
    private abstract class ContainerWithNestedConstants
    {
        public abstract class Group1
        {
            public const string A = "group1.a";
            public const string B = "group1.b";
        }

        public abstract class Group2
        {
            public const int Code = 42;
        }
    }

    [Fact]
    public void GetPublicConstants_ShouldReturnValues_FromNestedTypesOnly()
    {
        // Act
        var constants = ReflectionHelper.GetPublicConstants(typeof(ContainerWithNestedConstants));

        // Assert
        Assert.Contains("group1.a", constants);
        Assert.Contains("group1.b", constants);
        Assert.Contains("42", constants);
        Assert.Equal(3, constants.Count);
    }

    [Fact]
    public void GetPublicConstants_ShouldReturnEmpty_ForFlatClassWithNoNestedTypes()
    {
        // Act
        var constants = ReflectionHelper.GetPublicConstants(typeof(ClaimTypeConstants));

        // Assert
        Assert.Empty(constants);
    }
}
