using StarterKit.Catalog.Api.Domain.Categories;
using Xunit;

namespace Catalog.Tests.Domain.Categories;

/// <summary>
/// <see cref="Category"/> carries no domain invariant of its own — see its class doc comment —
/// so these cover only that state transitions land on the expected fields.
/// </summary>
public class CategoryTests
{
    [Fact]
    public void Create_ShouldSetNameAndParentCategoryId()
    {
        // Act
        var category = Category.Create("Electronics", "parent-1");

        // Assert
        Assert.Equal("Electronics", category.Name);
        Assert.Equal("parent-1", category.ParentCategoryId);
    }

    [Fact]
    public void Create_ShouldAllowNullParentCategoryId()
    {
        // Act
        var category = Category.Create("Electronics", null);

        // Assert
        Assert.Null(category.ParentCategoryId);
    }

    [Fact]
    public void Rename_ShouldMutateName()
    {
        // Arrange
        var category = Category.Create("Electronics", null);

        // Act
        category.Rename("Consumer Electronics");

        // Assert
        Assert.Equal("Consumer Electronics", category.Name);
    }

    [Fact]
    public void Move_ShouldMutateParentCategoryId()
    {
        // Arrange
        var category = Category.Create("Electronics", null);

        // Act
        category.Move("parent-2");

        // Assert
        Assert.Equal("parent-2", category.ParentCategoryId);
    }

    [Fact]
    public void Move_ShouldClearParentCategoryId_WhenGivenNull()
    {
        // Arrange
        var category = Category.Create("Electronics", "parent-1");

        // Act
        category.Move(null);

        // Assert
        Assert.Null(category.ParentCategoryId);
    }
}
