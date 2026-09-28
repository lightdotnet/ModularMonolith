namespace StarterKit.WebMvc.Web.DataTables;

/// <summary>
/// The "current row" a <c>&lt;data-table&gt;</c> exposes to its column templates. Declare one in the
/// view and pass it to the table's <c>row</c> attribute; the table sets <see cref="DataTableRow{T}.Item"/>
/// before rendering each cell, so a column's child content can read it:
/// <code>
/// @{ var row = new DataTableRow&lt;UserDto&gt;(); }
/// &lt;data-table source="Model.Table" row="row"&gt;
///     &lt;dt-column key="user" header="User"&gt;@row.Item.UserName&lt;/dt-column&gt;
/// &lt;/data-table&gt;
/// </code>
/// </summary>
public interface IDataTableRow
{
    void SetCurrent(
        object? item,
        int index);
}

public sealed class DataTableRow<T> : IDataTableRow
{
    public T Item { get; private set; } = default!;

    /// <summary>Zero-based index of the row within the current page.</summary>
    public int Index { get; private set; }

    public void SetCurrent(
        object? item,
        int index)
    {
        Item = (T)item!;
        Index = index;
    }
}
