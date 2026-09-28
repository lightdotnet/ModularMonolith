using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Primitives;

namespace StarterKit.WebMvc.Web.DataTables;

/// <summary>
/// Model of the <c>_Pagination</c> partial: page links keep every other query value (search,
/// sort, filters) so the no-JS fallback and reload/back behave like the scripted table.
/// </summary>
public sealed class DataTablePager(
    IDataTableSource source,
    string tableId,
    string basePath,
    IReadOnlyDictionary<string, StringValues> query)
{
    private const int SiblingCount = 2;

    public string TableId { get; } = tableId;

    public int Page { get; } = source.Page;

    public int PageSize { get; } = source.PageSize;

    public int TotalRecords { get; } = source.TotalRecords;

    public int TotalPages { get; } = Math.Max(source.TotalPages, 1);

    public int FirstRecord => TotalRecords == 0 ? 0 : ((Page - 1) * PageSize) + 1;

    public int LastRecord => Math.Min(Page * PageSize, TotalRecords);

    public IEnumerable<SelectListItem> PageSizeItems =>
        PagedQuery.PageSizeOptions.Select(size => new SelectListItem(
            size.ToString(CultureInfo.InvariantCulture),
            size.ToString(CultureInfo.InvariantCulture),
            size == PageSize));

    public string PageUrl(int page)
    {
        return DataTableUrl.Build(
            basePath,
            query,
            values =>
            {
                if (page <= 1)
                {
                    values.Remove(DataTableQueryKeys.Page);
                }
                else
                {
                    values[DataTableQueryKeys.Page] = page.ToString(CultureInfo.InvariantCulture);
                }
            });
    }

    /// <summary>
    /// Page numbers to show: always the first and last page plus <see cref="SiblingCount"/> pages
    /// around the current one; <c>null</c> marks an ellipsis gap (same rule as the admin client).
    /// </summary>
    public IReadOnlyList<int?> PageWindow()
    {
        var pages = new SortedSet<int> { 1, TotalPages };

        for (var i = Page - SiblingCount; i <= Page + SiblingCount; i++)
        {
            if (i >= 1 && i <= TotalPages)
            {
                pages.Add(i);
            }
        }

        var result = new List<int?>();
        var previous = 0;

        foreach (var page in pages)
        {
            if (previous > 0 && page - previous > 1)
            {
                result.Add(null);
            }

            result.Add(page);
            previous = page;
        }

        return result;
    }
}

internal static class DataTableUrl
{
    public static string Build(
        string basePath,
        IReadOnlyDictionary<string, StringValues> query,
        Action<Dictionary<string, StringValues>> mutate)
    {
        var values = new Dictionary<string, StringValues>(
            query,
            StringComparer.OrdinalIgnoreCase);

        mutate(values);

        var queryString = QueryString.Create(values.Where(pair => !StringValues.IsNullOrEmpty(pair.Value)));

        return basePath + queryString.ToUriComponent();
    }
}
