using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using StarterKit.WebMvc.Web.DataTables;
using Xunit;

namespace StarterKit.WebMvc.Tests.Web.DataTables;

public class PagedQueryModelBinderTests
{
    [Fact]
    public async Task Bind_ShouldReadValidValues()
    {
        // Act
        var query = await BindAsync(new Dictionary<string, StringValues>
        {
            ["page"] = "3",
            ["pageSize"] = "50",
            ["sort"] = " user.name ",
            ["dir"] = "DESC",
            ["q"] = "  alice  ",
        });

        // Assert
        Assert.Equal(3, query.Page);
        Assert.Equal(50, query.PageSize);
        Assert.Equal("user.name", query.Sort);
        Assert.Equal(SortDirection.Desc, query.Direction);
        Assert.Equal("alice", query.Search);
        Assert.True(query.IsSorted);
    }

    [Fact]
    public async Task Bind_WithNoValues_ShouldUseDefaults()
    {
        // Act
        var query = await BindAsync([]);

        // Assert
        Assert.Equal(PagedQuery.Default, query);
        Assert.False(query.IsSorted);
    }

    [Theory]
    [InlineData("0", 1)]
    [InlineData("-5", 1)]
    [InlineData("abc", 1)]
    [InlineData("7", 7)]
    public async Task Bind_ShouldClampPageToAtLeastOne(
        string page,
        int expected)
    {
        // Act
        var query = await BindAsync(new Dictionary<string, StringValues>
        {
            ["page"] = page,
        });

        // Assert
        Assert.Equal(expected, query.Page);
    }

    [Theory]
    [InlineData("10", 10)]
    [InlineData("100", 100)]
    [InlineData("25", PagedQuery.DefaultPageSize)]
    [InlineData("1000", PagedQuery.DefaultPageSize)]
    [InlineData("0", PagedQuery.DefaultPageSize)]
    [InlineData("x", PagedQuery.DefaultPageSize)]
    public async Task Bind_ShouldRestrictPageSizeToOptions(
        string pageSize,
        int expected)
    {
        // Act
        var query = await BindAsync(new Dictionary<string, StringValues>
        {
            ["pageSize"] = pageSize,
        });

        // Assert
        Assert.Equal(expected, query.PageSize);
    }

    [Theory]
    [InlineData("name", null)]
    [InlineData("name", "sideways")]
    [InlineData("name;drop", "asc")]
    [InlineData("na me", "asc")]
    [InlineData("", "asc")]
    public async Task Bind_ShouldDropSort_WhenKeyOrDirectionIsInvalid(
        string sort,
        string? direction)
    {
        // Arrange
        var values = new Dictionary<string, StringValues>
        {
            ["sort"] = sort,
        };

        if (direction is not null)
        {
            values["dir"] = direction;
        }

        // Act
        var query = await BindAsync(values);

        // Assert
        Assert.Null(query.Sort);
        Assert.Null(query.Direction);
        Assert.False(query.IsSorted);
    }

    [Fact]
    public async Task Bind_ShouldDropOverlongSortKey()
    {
        // Act
        var query = await BindAsync(new Dictionary<string, StringValues>
        {
            ["sort"] = new string('a', 65),
            ["dir"] = "asc",
        });

        // Assert
        Assert.Null(query.Sort);
    }

    [Fact]
    public async Task Bind_ShouldCapSearchLengthAndNullBlankSearch()
    {
        // Act
        var longQuery = await BindAsync(new Dictionary<string, StringValues>
        {
            ["q"] = new string('x', 300),
        });

        var blankQuery = await BindAsync(new Dictionary<string, StringValues>
        {
            ["q"] = "   ",
        });

        // Assert
        Assert.Equal(256, longQuery.Search!.Length);
        Assert.Null(blankQuery.Search);
    }

    private static async Task<PagedQuery> BindAsync(Dictionary<string, StringValues> values)
    {
        var context = new DefaultModelBindingContext
        {
            ModelName = string.Empty,
            ValueProvider = new QueryStringValueProvider(
                BindingSource.Query,
                new QueryCollection(values),
                CultureInfo.InvariantCulture),
        };

        await new PagedQueryModelBinder().BindModelAsync(context);

        Assert.True(context.Result.IsModelSet);

        return Assert.IsType<PagedQuery>(context.Result.Model);
    }
}
