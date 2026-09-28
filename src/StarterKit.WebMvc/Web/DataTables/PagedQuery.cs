using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace StarterKit.WebMvc.Web.DataTables;

public enum SortDirection
{
    Asc,
    Desc,
}

/// <summary>
/// Query-string state shared by every data table: <c>page</c>, <c>pageSize</c>, <c>sort</c>,
/// <c>dir</c> and <c>q</c>. Bound by <see cref="PagedQueryModelBinder"/> (no prefix), so a
/// controller action or page handler just declares a <c>PagedQuery query</c> parameter; screen
/// specific filters are bound alongside it as ordinary parameters.
/// </summary>
[ModelBinder(typeof(PagedQueryModelBinder))]
public sealed record PagedQuery(
    int Page,
    int PageSize,
    string? Sort,
    SortDirection? Direction,
    string? Search)
{
    public const int DefaultPageSize = 20;

    public static readonly IReadOnlyList<int> PageSizeOptions = [10, 20, 50, 100];

    public static PagedQuery Default { get; } = new(
        1,
        DefaultPageSize,
        null,
        null,
        null);

    public bool IsSorted => !string.IsNullOrEmpty(Sort) && Direction is not null;
}

/// <summary>Query-string keys of <see cref="PagedQuery"/>; <c>data-table.js</c> uses the same names.</summary>
public static class DataTableQueryKeys
{
    public const string Page = "page";

    public const string PageSize = "pageSize";

    public const string Sort = "sort";

    public const string Direction = "dir";

    public const string Search = "q";
}

/// <summary>
/// Binds <see cref="PagedQuery"/> from the query string, clamping everything to safe values:
/// page ≥ 1, page size from <see cref="PagedQuery.PageSizeOptions"/>, a plain identifier as the
/// sort key (only kept together with a valid direction), and a trimmed, length-capped search term.
/// </summary>
public sealed partial class PagedQueryModelBinder : IModelBinder
{
    private const int MaxSearchLength = 256;

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var values = bindingContext.ValueProvider;

        var page = ReadInt(
            values,
            DataTableQueryKeys.Page);

        var pageSize = ReadInt(
            values,
            DataTableQueryKeys.PageSize);

        var sort = values.GetValue(DataTableQueryKeys.Sort).FirstValue?.Trim();

        var direction = values.GetValue(DataTableQueryKeys.Direction).FirstValue?.Trim().ToLowerInvariant() switch
        {
            "asc" => SortDirection.Asc,
            "desc" => SortDirection.Desc,
            _ => (SortDirection?)null,
        };

        var search = values.GetValue(DataTableQueryKeys.Search).FirstValue?.Trim();

        if (search is { Length: > MaxSearchLength })
        {
            search = search[..MaxSearchLength];
        }

        var isValidSort = !string.IsNullOrEmpty(sort)
            && SortKeyPattern().IsMatch(sort)
            && direction is not null;

        bindingContext.Result = ModelBindingResult.Success(new PagedQuery(
            page is > 0 ? page.Value : 1,
            pageSize is not null && PagedQuery.PageSizeOptions.Contains(pageSize.Value)
                ? pageSize.Value
                : PagedQuery.DefaultPageSize,
            isValidSort ? sort : null,
            isValidSort ? direction : null,
            string.IsNullOrEmpty(search) ? null : search));

        return Task.CompletedTask;
    }

    private static int? ReadInt(
        IValueProvider values,
        string key)
    {
        return int.TryParse(
            values.GetValue(key).FirstValue,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : null;
    }

    [GeneratedRegex(@"^[A-Za-z0-9_.]{1,64}$")]
    private static partial Regex SortKeyPattern();
}
