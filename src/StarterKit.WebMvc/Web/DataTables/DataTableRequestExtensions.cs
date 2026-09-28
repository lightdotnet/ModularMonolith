namespace StarterKit.WebMvc.Web.DataTables;

/// <summary>
/// The one place that interprets the <see cref="FragmentHeader"/> sent by <c>data-table.js</c> —
/// used by the page/controller bases (return the partial) and the <c>&lt;data-table&gt;</c> tag
/// helper (emit only the body of the table the header names).
/// </summary>
public static class DataTableRequestExtensions
{
    /// <summary>Header <c>data-table.js</c> sends (with the table id) when it asks for just the table body.</summary>
    public const string FragmentHeader = "X-DataTable";

    /// <summary>
    /// True when the request comes from <c>data-table.js</c> and wants a body fragment (rows +
    /// pager) instead of the full page. With <paramref name="tableId"/>, only when the fragment is
    /// for that table (ids compare ordinally, as in the DOM).
    /// </summary>
    public static bool IsDataTableFragment(
        this HttpRequest request,
        string? tableId = null)
    {
        var requestedId = request.DataTableFragmentId();

        if (string.IsNullOrEmpty(requestedId))
        {
            return false;
        }

        return tableId is null
            || string.Equals(
                requestedId,
                tableId,
                StringComparison.Ordinal);
    }

    /// <summary>The table id named by the fragment header, or <c>null</c> for a normal request.</summary>
    public static string? DataTableFragmentId(this HttpRequest request) =>
        request.Headers[FragmentHeader].FirstOrDefault();
}
