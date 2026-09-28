using StarterKit.WebMvc.Web.DataTables;
using Xunit;

namespace StarterKit.WebMvc.Tests.Web.DataTables;

public class DataTableResultTests
{
    private sealed record Row(
        string Name,
        int? Rank);

    private static readonly IReadOnlyDictionary<string, Func<Row, object?>> SortKeys =
        new Dictionary<string, Func<Row, object?>>
        {
            ["name"] = row => row.Name,
            ["rank"] = row => row.Rank,
        };

    private static readonly Row[] Rows =
    [
        new("charlie", 3),
        new("Alpha", 1),
        new("bravo", null),
        new("delta", 4),
        new("echo", 2),
    ];

    [Fact]
    public void FromList_ShouldPageResults()
    {
        // Act
        var result = FromList(Query(
            page: 2,
            pageSize: 2));

        // Assert
        Assert.Equal(5, result.TotalRecords);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.Page);
        Assert.Equal(["bravo", "delta"], result.Records.Select(row => row.Name));
        Assert.Null(result.Error);
    }

    [Fact]
    public void FromList_ShouldClampPageBeyondLastToLastPage()
    {
        // Act
        var result = FromList(Query(
            page: 99,
            pageSize: 2));

        // Assert
        Assert.Equal(3, result.Page);
        Assert.Equal(["echo"], result.Records.Select(row => row.Name));
    }

    [Fact]
    public void FromList_WithNoRows_ShouldStayOnFirstPage()
    {
        // Act
        var result = DataTableResult<Row>.FromList(
            [],
            Query(page: 4),
            (_, _) => true,
            SortKeys);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(0, result.TotalRecords);
        Assert.Equal(0, result.TotalPages);
        Assert.Empty(result.Records);
    }

    [Fact]
    public void FromList_ShouldSortStringsCaseInsensitively()
    {
        // Act
        var result = FromList(Query(
            sort: "name",
            direction: SortDirection.Asc));

        // Assert
        Assert.Equal(["Alpha", "bravo", "charlie", "delta", "echo"], result.Records.Select(row => row.Name));
    }

    [Fact]
    public void FromList_ShouldSortDescendingWithNullsLast()
    {
        // Act
        var result = FromList(Query(
            sort: "rank",
            direction: SortDirection.Desc));

        // Assert: nulls compare first, so they end up last when descending.
        Assert.Equal([4, 3, 2, 1, (int?)null], result.Records.Select(row => row.Rank));
    }

    [Fact]
    public void FromList_WithUnknownSortKey_ShouldKeepOriginalOrder()
    {
        // Act
        var result = FromList(Query(
            sort: "unknown",
            direction: SortDirection.Asc));

        // Assert
        Assert.Equal(Rows.Select(row => row.Name), result.Records.Select(row => row.Name));
    }

    [Fact]
    public void FromList_ShouldApplySearchBeforePaging()
    {
        // Act
        var result = DataTableResult<Row>.FromList(
            Rows,
            Query(
                pageSize: 10,
                search: "a"),
            (row, term) => row.Name.Contains(
                term,
                StringComparison.OrdinalIgnoreCase),
            SortKeys);

        // Assert
        Assert.Equal(4, result.TotalRecords);
        Assert.DoesNotContain(result.Records, row => row.Name == "echo");
    }

    private static DataTableResult<Row> FromList(PagedQuery query) =>
        DataTableResult<Row>.FromList(
            Rows,
            query,
            (_, _) => true,
            SortKeys);

    private static PagedQuery Query(
        int page = 1,
        int pageSize = 10,
        string? sort = null,
        SortDirection? direction = null,
        string? search = null) =>
        new(
            page,
            pageSize,
            sort,
            direction,
            search);
}
