using System.Collections;
using Light.Contracts;
using StarterKit.WebMvc.Services.Http;

namespace StarterKit.WebMvc.Web.DataTables;

/// <summary>
/// What the <c>&lt;data-table&gt;</c> tag helper renders: one page of rows plus the paging totals,
/// or an error message in place of the rows.
/// </summary>
public interface IDataTableSource
{
    IEnumerable Records { get; }

    int Page { get; }

    int PageSize { get; }

    int TotalRecords { get; }

    int TotalPages { get; }

    /// <summary>Set when the backend call failed; the table then shows an error state instead of rows.</summary>
    string? Error { get; }

    PagedQuery Query { get; }
}

public sealed class DataTableResult<T> : IDataTableSource
{
    private DataTableResult(
        IReadOnlyList<T> records,
        int totalRecords,
        PagedQuery query,
        string? error)
    {
        Records = records;
        TotalRecords = totalRecords;
        Query = query;
        Error = error;
    }

    public IReadOnlyList<T> Records { get; }

    IEnumerable IDataTableSource.Records => Records;

    public int Page => Query.Page;

    public int PageSize => Query.PageSize;

    public int TotalRecords { get; }

    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling(TotalRecords / (double)PageSize)
        : 0;

    public string? Error { get; }

    public PagedQuery Query { get; }

    public static DataTableResult<T> Empty(PagedQuery query) => new(
        [],
        0,
        query,
        null);

    public static DataTableResult<T> Failure(
        string error,
        PagedQuery query)
    {
        return new DataTableResult<T>(
            [],
            0,
            query,
            error);
    }

    /// <summary>Wraps one server-paged page returned by the backend.</summary>
    public static DataTableResult<T> FromApi(
        ApiResult<Paged<T>> result,
        PagedQuery query)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            return Failure(
                result.Message,
                query);
        }

        return new DataTableResult<T>(
            result.Data.Records.ToList(),
            result.Data.TotalRecords,
            query with
            {
                Page = Math.Max(result.Data.PageNumber, 1),
            },
            null);
    }

    /// <summary>
    /// Searches, sorts and pages a full list in memory — for backend endpoints that are not paged
    /// (e.g. <c>GET role</c>). <paramref name="sortKeys"/> maps each sortable column key to the
    /// value it sorts by; an unknown sort key leaves the original order.
    /// </summary>
    public static DataTableResult<T> FromList(
        IEnumerable<T> source,
        PagedQuery query,
        Func<T, string, bool> matchesSearch,
        IReadOnlyDictionary<string, Func<T, object?>> sortKeys)
    {
        var items = source;

        if (!string.IsNullOrEmpty(query.Search))
        {
            items = items.Where(item => matchesSearch(
                item,
                query.Search));
        }

        if (query is { IsSorted: true, Sort: not null }
            && sortKeys.TryGetValue(query.Sort, out var sortKey))
        {
            items = query.Direction == SortDirection.Desc
                ? items.OrderByDescending(sortKey, NaturalComparer.Instance)
                : items.OrderBy(sortKey, NaturalComparer.Instance);
        }

        var all = items.ToList();
        var totalPages = Math.Max(
            (int)Math.Ceiling(all.Count / (double)query.PageSize),
            1);

        var page = Math.Min(
            query.Page,
            totalPages);

        return new DataTableResult<T>(
            all.Skip((page - 1) * query.PageSize).Take(query.PageSize).ToList(),
            all.Count,
            query with { Page = page },
            null);
    }

    /// <summary>Orders nulls first and strings case-insensitively; everything else by its own comparison.</summary>
    private sealed class NaturalComparer : IComparer<object?>
    {
        public static readonly NaturalComparer Instance = new();

        public int Compare(
            object? x,
            object? y)
        {
            if (x is null || y is null)
            {
                return x is null ? (y is null ? 0 : -1) : 1;
            }

            if (x is string left && y is string right)
            {
                return StringComparer.OrdinalIgnoreCase.Compare(
                    left,
                    right);
            }

            return Comparer<object>.Default.Compare(
                x,
                y);
        }
    }
}
