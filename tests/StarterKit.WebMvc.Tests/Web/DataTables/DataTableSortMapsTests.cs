using StarterKit.Identity.Contracts;
using StarterKit.Notifications.Contracts.SystemNotifications;
using StarterKit.WebMvc.Web.DataTables;
using Xunit;

namespace StarterKit.WebMvc.Tests.Web.DataTables;

public class DataTableSortMapsTests
{
    [Theory]
    [InlineData("user", SortDirection.Asc, SearchUserSortFields.UserName, "asc")]
    [InlineData("NAME", SortDirection.Desc, SearchUserSortFields.FullName, "desc")]
    [InlineData("email", SortDirection.Asc, SearchUserSortFields.Email, "asc")]
    [InlineData("status", SortDirection.Desc, SearchUserSortFields.Status, "desc")]
    public void ForUsers_ShouldMapAllowedColumns(
        string column,
        SortDirection direction,
        string expectedSortBy,
        string expectedDirection)
    {
        // Act
        var sort = DataTableSortMaps.ForUsers(Sorted(
            column,
            direction));

        // Assert
        Assert.Equal(
            new BackendSort(
                expectedSortBy,
                expectedDirection),
            sort);
    }

    [Theory]
    [InlineData("created", NotificationSortFields.Created)]
    [InlineData("title", NotificationSortFields.Title)]
    [InlineData("status", NotificationSortFields.Status)]
    [InlineData("from", NotificationSortFields.FromName)]
    public void ForNotifications_ShouldMapAllowedColumns(
        string column,
        string expectedSortBy)
    {
        // Act
        var sort = DataTableSortMaps.ForNotifications(Sorted(
            column,
            SortDirection.Asc));

        // Assert
        Assert.Equal(expectedSortBy, sort!.SortBy);
        Assert.Equal("asc", sort.SortDirection);
    }

    [Fact]
    public void ForUsers_WithUnknownColumn_ShouldNotSort()
    {
        // Act
        var sort = DataTableSortMaps.ForUsers(Sorted(
            "passwordHash",
            SortDirection.Asc));

        // Assert
        Assert.Null(sort);
    }

    [Fact]
    public void ForNotifications_WhenUnsorted_ShouldNotSort()
    {
        // Act
        var sort = DataTableSortMaps.ForNotifications(PagedQuery.Default);

        // Assert
        Assert.Null(sort);
    }

    [Fact]
    public void Resolve_WhenMappedNameIsOutsideAllowList_ShouldNotSort()
    {
        // Arrange
        var map = new Dictionary<string, string>
        {
            ["secret"] = "passwordHash",
        };

        // Act
        var sort = DataTableSortMaps.Resolve(
            Sorted(
                "secret",
                SortDirection.Asc),
            map,
            SearchUserSortFields.All);

        // Assert
        Assert.Null(sort);
    }

    private static PagedQuery Sorted(
        string column,
        SortDirection direction) =>
        PagedQuery.Default with
        {
            Sort = column,
            Direction = direction,
        };
}
