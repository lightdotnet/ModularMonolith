using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Primitives;
using StarterKit.WebMvc.Authorization;
using StarterKit.WebMvc.Web.DataTables;

namespace StarterKit.WebMvc.TagHelpers;

/// <summary>
/// Server-paged data table. Columns are declared once, in the table's partial view, and that same
/// partial serves both the first (full-page) render and every refresh: <c>data-table.js</c> requests
/// the table's source URL with the table state in the query string and an <c>X-DataTable: {id}</c>
/// header, the handler returns the partial, and — when the header names this table's <c>id</c> —
/// this tag helper emits only the body fragment (rows + pager), without the toolbar.
/// <code>
/// @{ var row = new DataTableRow&lt;UserDto&gt;(); }
/// &lt;data-table id="users" source="Model.Table" row="row" search-placeholder="Search users..."&gt;
///     &lt;dt-action href="@Url.Page("Create")" icon="person-plus" permission="@IdentityPermissions.Users.Create"&gt;Create user&lt;/dt-action&gt;
///     &lt;dt-column key="userName" header="Username" field="UserName" sortable="true" /&gt;
///     &lt;dt-column key="status" header="Status"&gt;&lt;status-badge value="@row.Item.Status" /&gt;&lt;/dt-column&gt;
///     &lt;dt-column key="actions" actions="true"&gt;…icon buttons…&lt;/dt-column&gt;
/// &lt;/data-table&gt;
/// </code>
/// A column with <c>field</c> renders that property (formatted by type/<c>format</c>); otherwise its
/// child content is the per-row cell template, reading the current row through <c>row</c>.
/// <para>
/// Rendering: the child content is executed once as a registration pass — columns (and whether the
/// user may see each one), actions and filters are recorded — and then once more per row, with
/// <c>row</c> pointing at that record, where each visible <c>&lt;dt-column&gt;</c> renders its cell
/// during its own execution. A column's child content must never be captured and replayed later:
/// Razor reuses the child-content buffers between executions.
/// </para>
/// <para>
/// Limits that follow from this:
/// <list type="bullet">
/// <item>the set of <c>&lt;dt-column&gt;</c> tags must be the same on every pass — no <c>@if</c>
/// around a column that depends on the row (use <c>permission</c> to drop a column, or render an
/// empty cell instead). A row whose cell count differs from the column count throws in Development
/// and is logged as an error (with the row padded/truncated) elsewhere;</item>
/// <item>attributes of <c>&lt;dt-column&gt;</c>/<c>&lt;dt-action&gt;</c> (header, css, permission,
/// href, …) are read during the registration pass only, where no row is current — they must not
/// reference <c>row</c>. Only a column's child content (and <c>row-css</c>) is per-row.</item>
/// </list>
/// </para>
/// </summary>
[HtmlTargetElement("data-table")]
[RestrictChildren("dt-column", "dt-action", "dt-filters")]
public sealed class DataTableTagHelper(
    IHtmlHelper htmlHelper,
    IHostEnvironment environment,
    ILogger<DataTableTagHelper> logger)
    : TagHelper
{
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    /// <summary>Element id; also identifies the table in fragment requests.</summary>
    public string Id { get; set; } = "data-table";

    public IDataTableSource Source { get; set; } = null!;

    /// <summary>Current-row holder read by column templates (see <see cref="DataTableRow{T}"/>).</summary>
    public IDataTableRow? Row { get; set; }

    /// <summary>Per-row CSS classes, evaluated after <see cref="Row"/> is set (e.g. emphasise unread rows).</summary>
    public Func<string?>? RowCss { get; set; }

    /// <summary>
    /// URL the table refreshes from; defaults to the current path (the page's own GET handler).
    /// The browser address bar only mirrors the table state when the table refreshes from the
    /// page's own path.
    /// </summary>
    public string? Url { get; set; }

    public bool Searchable { get; set; } = true;

    public string SearchPlaceholder { get; set; } = "Search...";

    public bool Refresh { get; set; } = true;

    /// <summary>Shows an Export button that downloads the rows currently shown as CSV.</summary>
    public bool Export { get; set; }

    public string? ExportName { get; set; }

    public string EmptyIcon { get; set; } = "inbox";

    public string EmptyTitle { get; set; } = "No records";

    public string? EmptyDescription { get; set; }

    public override async Task ProcessAsync(
        TagHelperContext context,
        TagHelperOutput output)
    {
        var table = new DataTableDefinition();
        context.Items[typeof(DataTableDefinition)] = table;

        // Registration pass: the child tags only declare columns/actions/filters.
        await output.GetChildContentAsync();

        var request = ViewContext.HttpContext.Request;
        var currentPath = $"{request.PathBase}{request.Path}";
        var basePath = Url ?? currentPath;
        var query = request.Query.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);

        var body = await RenderBodyAsync(
            table,
            output,
            basePath,
            query);

        if (request.IsDataTableFragment(Id))
        {
            output.TagName = null;
            output.Content.SetHtmlContent(body);
            return;
        }

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("id", Id);
        output.Attributes.SetAttribute("class", "data-table");
        output.Attributes.SetAttribute("data-dt", string.Empty);
        output.Attributes.SetAttribute("data-dt-source", basePath);
        output.Attributes.SetAttribute("data-dt-export-name", ExportName ?? Id);
        output.Attributes.SetAttribute("aria-busy", "false");

        if (string.Equals(basePath, currentPath, StringComparison.OrdinalIgnoreCase))
        {
            output.Attributes.SetAttribute("data-dt-sync-url", "true");
        }

        output.Content.SetHtmlContent(RenderToolbar(table));

        // Polite, visually hidden summary ("Showing 1–20 of 135") updated by data-table.js after
        // each refresh — announcing the whole swapped body would read every row.
        var status = new TagBuilder("div");
        status.AddCssClass("visually-hidden");
        status.Attributes["aria-live"] = "polite";
        status.Attributes["aria-atomic"] = "true";
        status.Attributes["data-dt-status"] = string.Empty;
        output.Content.AppendHtml(status);

        var bodyWrapper = new TagBuilder("div");
        bodyWrapper.AddCssClass("dt-body");
        bodyWrapper.Attributes["data-dt-body"] = string.Empty;
        bodyWrapper.InnerHtml.AppendHtml(body);
        output.Content.AppendHtml(bodyWrapper);
    }

    private IHtmlContent RenderToolbar(DataTableDefinition table)
    {
        var toolbar = new TagBuilder("div");
        toolbar.AddCssClass("dt-toolbar d-flex flex-column gap-2 mb-2");

        if (table.Actions.Count > 0)
        {
            var actions = new TagBuilder("div");
            actions.AddCssClass("d-flex flex-wrap gap-2");

            foreach (var action in table.Actions)
            {
                var link = new TagBuilder("a");
                link.AddCssClass($"btn btn-{action.Variant}");
                link.Attributes["href"] = action.Href;

                if (!string.IsNullOrEmpty(action.Icon))
                {
                    link.InnerHtml.AppendHtml(IconTagHelper.Render(action.Icon, "me-2"));
                }

                link.InnerHtml.AppendHtml(action.Label);
                actions.InnerHtml.AppendHtml(link);
            }

            toolbar.InnerHtml.AppendHtml(actions);
        }

        var row = new TagBuilder("div");
        row.AddCssClass("d-flex flex-column flex-md-row gap-2 align-items-md-center justify-content-between");

        var searchArea = new TagBuilder("div");
        searchArea.AddCssClass("d-flex flex-column flex-sm-row flex-wrap gap-2 flex-grow-1 align-items-sm-center");

        if (Searchable)
        {
            searchArea.InnerHtml.AppendHtml(RenderSearch());
        }

        if (table.Filters is not null)
        {
            var filters = new TagBuilder("div");
            filters.AddCssClass("dt-filters d-flex flex-column flex-sm-row flex-wrap gap-2");
            filters.Attributes["data-dt-filters"] = string.Empty;
            filters.InnerHtml.AppendHtml(table.Filters);
            searchArea.InnerHtml.AppendHtml(filters);
        }

        row.InnerHtml.AppendHtml(searchArea);
        row.InnerHtml.AppendHtml(RenderButtons(table));
        toolbar.InnerHtml.AppendHtml(row);

        return toolbar;
    }

    private IHtmlContent RenderSearch()
    {
        var group = new TagBuilder("div");
        group.AddCssClass("input-group dt-search");

        var addon = new TagBuilder("span");
        addon.AddCssClass("input-group-text bg-body");
        addon.InnerHtml.AppendHtml(IconTagHelper.Render("search"));
        group.InnerHtml.AppendHtml(addon);

        var input = new TagBuilder("input");
        input.TagRenderMode = TagRenderMode.SelfClosing;
        input.AddCssClass("form-control");
        input.Attributes["type"] = "search";
        input.Attributes["name"] = DataTableQueryKeys.Search;
        input.Attributes["value"] = Source.Query.Search ?? string.Empty;
        input.Attributes["placeholder"] = SearchPlaceholder;
        input.Attributes["aria-label"] = SearchPlaceholder;
        input.Attributes["autocomplete"] = "off";
        input.Attributes["data-dt-search"] = string.Empty;
        group.InnerHtml.AppendHtml(input);

        return group;
    }

    private IHtmlContent RenderButtons(DataTableDefinition table)
    {
        var hideable = table.Columns.Where(column => column.Hideable).ToList();

        var group = new TagBuilder("div");
        group.AddCssClass("btn-group btn-group-sm flex-shrink-0 align-self-end align-self-md-auto");
        group.Attributes["role"] = "group";
        group.Attributes["aria-label"] = "Table actions";

        if (Export)
        {
            group.InnerHtml.AppendHtml(ToolbarButton(
                "data-dt-export",
                "download",
                "Export"));
        }

        if (Refresh)
        {
            group.InnerHtml.AppendHtml(ToolbarButton(
                "data-dt-refresh",
                "arrow-clockwise",
                "Refresh"));
        }

        if (hideable.Count > 0)
        {
            var dropdown = new TagBuilder("div");
            dropdown.AddCssClass("btn-group btn-group-sm");
            dropdown.Attributes["role"] = "group";

            // The text label is hidden below sm, so the accessible name comes from aria-label.
            var toggle = new TagBuilder("button");
            toggle.AddCssClass("btn btn-outline-secondary dropdown-toggle");
            toggle.Attributes["type"] = "button";
            toggle.Attributes["data-bs-toggle"] = "dropdown";
            toggle.Attributes["data-bs-auto-close"] = "outside";
            toggle.Attributes["aria-expanded"] = "false";
            toggle.Attributes["aria-label"] = "Columns";
            toggle.Attributes["title"] = "Columns";
            toggle.InnerHtml.AppendHtml(IconTagHelper.Render("layout-three-columns", "me-sm-1"));
            toggle.InnerHtml.AppendHtml("<span class=\"d-none d-sm-inline\">Columns</span>");
            dropdown.InnerHtml.AppendHtml(toggle);

            var menu = new TagBuilder("div");
            menu.AddCssClass("dropdown-menu dropdown-menu-end p-2 dt-columns-menu");

            foreach (var column in hideable)
            {
                var checkId = $"{Id}-col-{column.Key}";

                var check = new TagBuilder("div");
                check.AddCssClass("form-check");

                var input = new TagBuilder("input");
                input.TagRenderMode = TagRenderMode.SelfClosing;
                input.AddCssClass("form-check-input");
                input.Attributes["type"] = "checkbox";
                input.Attributes["id"] = checkId;
                input.Attributes["checked"] = "checked";
                input.Attributes["data-dt-toggle-col"] = column.Key;
                check.InnerHtml.AppendHtml(input);

                var label = new TagBuilder("label");
                label.AddCssClass("form-check-label text-nowrap");
                label.Attributes["for"] = checkId;
                label.InnerHtml.Append(column.Header);
                check.InnerHtml.AppendHtml(label);

                menu.InnerHtml.AppendHtml(check);
            }

            dropdown.InnerHtml.AppendHtml(menu);
            group.InnerHtml.AppendHtml(dropdown);
        }

        return group;
    }

    /// <summary>An icon button whose text label is hidden below sm — <c>aria-label</c> keeps its accessible name.</summary>
    private static TagBuilder ToolbarButton(
        string dataAttribute,
        string icon,
        string label)
    {
        var button = new TagBuilder("button");
        button.AddCssClass("btn btn-outline-secondary");
        button.Attributes["type"] = "button";
        button.Attributes[dataAttribute] = string.Empty;
        button.Attributes["title"] = label;
        button.Attributes["aria-label"] = label;
        button.InnerHtml.AppendHtml(IconTagHelper.Render(icon, "me-sm-1"));

        var text = new TagBuilder("span");
        text.AddCssClass("d-none d-sm-inline");
        text.InnerHtml.Append(label);
        button.InnerHtml.AppendHtml(text);

        return button;
    }

    private async Task<IHtmlContent> RenderBodyAsync(
        DataTableDefinition table,
        TagHelperOutput output,
        string basePath,
        IReadOnlyDictionary<string, StringValues> query)
    {
        var body = new HtmlContentBuilder();

        if (Source.Error is not null)
        {
            body.AppendHtml(RenderError(Source.Error));
            return body;
        }

        var scroll = new TagBuilder("div");
        scroll.AddCssClass("dt-scroll border rounded");

        // Explicit ARIA roles: below md the table collapses to display:block cards (site.css),
        // which strips the implicit table semantics in several browsers.
        var htmlTable = new TagBuilder("table");
        htmlTable.AddCssClass("table table-hover align-middle mb-0 dt-table");
        htmlTable.Attributes["role"] = "table";

        htmlTable.InnerHtml.AppendHtml(RenderHead(
            table,
            basePath,
            query));

        htmlTable.InnerHtml.AppendHtml(await RenderRowsAsync(
            table,
            output));

        scroll.InnerHtml.AppendHtml(htmlTable);
        body.AppendHtml(scroll);

        ((IViewContextAware)htmlHelper).Contextualize(ViewContext);

        body.AppendHtml(await htmlHelper.PartialAsync(
            "/Views/Shared/_Pagination.cshtml",
            new DataTablePager(
                Source,
                Id,
                basePath,
                query)));

        return body;
    }

    private IHtmlContent RenderHead(
        DataTableDefinition table,
        string basePath,
        IReadOnlyDictionary<string, StringValues> query)
    {
        var head = new TagBuilder("thead");
        head.AddCssClass("table-light");
        head.Attributes["role"] = "rowgroup";

        var tr = new TagBuilder("tr");
        tr.Attributes["role"] = "row";

        foreach (var column in table.Columns)
        {
            var th = new TagBuilder("th");
            th.Attributes["scope"] = "col";
            th.Attributes["role"] = "columnheader";
            th.Attributes["data-col"] = column.Key;
            th.AddCssClass($"text-nowrap {column.Css} {(column.IsActions ? "text-end dt-actions" : null)}".Trim());

            if (!column.Export)
            {
                th.Attributes["data-dt-export"] = "false";
            }

            if (!column.Sortable)
            {
                if (column.IsActions && string.IsNullOrEmpty(column.Header))
                {
                    th.InnerHtml.AppendHtml("<span class=\"visually-hidden\">Actions</span>");
                }
                else
                {
                    th.InnerHtml.Append(column.Header);
                }

                tr.InnerHtml.AppendHtml(th);
                continue;
            }

            var isCurrent = string.Equals(Source.Query.Sort, column.Key, StringComparison.OrdinalIgnoreCase)
                && Source.Query.Direction is not null;

            // none → asc → desc → none, like the admin client.
            SortDirection? next = !isCurrent
                ? SortDirection.Asc
                : Source.Query.Direction == SortDirection.Asc ? SortDirection.Desc : null;

            if (isCurrent)
            {
                th.Attributes["aria-sort"] = Source.Query.Direction == SortDirection.Asc ? "ascending" : "descending";
            }

            var link = new TagBuilder("a");
            link.AddCssClass("dt-sort link-body-emphasis text-decoration-none d-inline-flex align-items-center gap-1");
            link.Attributes["data-dt-nav"] = string.Empty;
            link.Attributes["href"] = DataTableUrl.Build(
                basePath,
                query,
                values =>
                {
                    values.Remove(DataTableQueryKeys.Page);

                    if (next is null)
                    {
                        values.Remove(DataTableQueryKeys.Sort);
                        values.Remove(DataTableQueryKeys.Direction);
                    }
                    else
                    {
                        values[DataTableQueryKeys.Sort] = column.Key;
                        values[DataTableQueryKeys.Direction] = next == SortDirection.Asc ? "asc" : "desc";
                    }
                });

            link.InnerHtml.Append(column.Header);
            link.InnerHtml.AppendHtml(IconTagHelper.Render(
                !isCurrent ? "arrow-down-up" : Source.Query.Direction == SortDirection.Asc ? "arrow-up" : "arrow-down",
                isCurrent ? "small" : "small opacity-50"));

            th.InnerHtml.AppendHtml(link);
            tr.InnerHtml.AppendHtml(th);
        }

        head.InnerHtml.AppendHtml(tr);
        return head;
    }

    private async Task<IHtmlContent> RenderRowsAsync(
        DataTableDefinition table,
        TagHelperOutput output)
    {
        var tbody = new TagBuilder("tbody");
        tbody.Attributes["role"] = "rowgroup";

        var index = 0;
        var mismatchLogged = false;

        foreach (var record in Source.Records)
        {
            Row?.SetCurrent(
                record,
                index);

            // Row pass: each visible registered column renders its cell for this record, in order.
            table.BeginRow(record);
            await output.GetChildContentAsync(useCachedResult: false);
            var row = table.EndRow();

            if (row.Cells.Count != table.Columns.Count || row.DeclaredColumns != table.DeclaredColumnCount)
            {
                ReportRowMismatch(
                    table,
                    row,
                    index,
                    ref mismatchLogged);
            }

            index++;

            var tr = new TagBuilder("tr");
            tr.Attributes["role"] = "row";
            tr.Attributes["data-dt-row"] = string.Empty;

            var rowCss = RowCss?.Invoke();

            if (!string.IsNullOrWhiteSpace(rowCss))
            {
                tr.AddCssClass(rowCss);
            }

            for (var i = 0; i < table.Columns.Count; i++)
            {
                var column = table.Columns[i];
                var cell = i < row.Cells.Count ? row.Cells[i] : HtmlString.Empty;

                var td = new TagBuilder("td");
                td.Attributes["role"] = "cell";
                td.Attributes["data-col"] = column.Key;
                td.Attributes["data-label"] = column.IsActions ? string.Empty : column.Header;

                if (!string.IsNullOrWhiteSpace(column.Css))
                {
                    td.AddCssClass(column.Css);
                }

                if (column.IsActions)
                {
                    td.AddCssClass("text-end dt-actions");

                    var wrapper = new TagBuilder("div");
                    wrapper.AddCssClass("d-flex justify-content-end align-items-center gap-1");
                    wrapper.InnerHtml.AppendHtml(cell);
                    td.InnerHtml.AppendHtml(wrapper);
                }
                else
                {
                    td.InnerHtml.AppendHtml(cell);
                }

                tr.InnerHtml.AppendHtml(td);
            }

            tbody.InnerHtml.AppendHtml(tr);
        }

        if (index == 0)
        {
            var tr = new TagBuilder("tr");
            tr.Attributes["role"] = "row";

            var td = new TagBuilder("td");
            td.Attributes["role"] = "cell";
            td.AddCssClass("dt-empty-cell");
            td.Attributes["colspan"] = Math.Max(table.Columns.Count, 1).ToString(CultureInfo.InvariantCulture);
            td.InnerHtml.AppendHtml(EmptyStateTagHelper.Render(
                EmptyIcon,
                EmptyTitle,
                EmptyDescription));

            tr.InnerHtml.AppendHtml(td);
            tbody.InnerHtml.AppendHtml(tr);
        }

        return tbody;
    }

    /// <summary>
    /// A row rendered a different set of <c>&lt;dt-column&gt;</c> tags than the registration pass —
    /// cells would land under the wrong headers. A view bug: fail loudly in Development, log once
    /// per render elsewhere (the row is then padded/truncated to the column count).
    /// </summary>
    private void ReportRowMismatch(
        DataTableDefinition table,
        DataTableRowCells row,
        int rowIndex,
        ref bool alreadyLogged)
    {
        var message = $"<data-table id=\"{Id}\">: row {rowIndex} rendered {row.Cells.Count} cell(s) from {row.DeclaredColumns} <dt-column> tag(s), "
            + $"but {table.Columns.Count} visible column(s) were registered from {table.DeclaredColumnCount} tag(s). "
            + "<dt-column> tags must not be conditional on the row.";

        if (environment.IsDevelopment())
        {
            throw new InvalidOperationException(message);
        }

        if (alreadyLogged)
        {
            return;
        }

        alreadyLogged = true;
        logger.LogError(
            "Data table column mismatch on {Path}: {Message}",
            ViewContext.HttpContext.Request.Path,
            message);
    }

    private static IHtmlContent RenderError(string message)
    {
        var alert = new TagBuilder("div");
        alert.AddCssClass("alert alert-danger d-flex flex-column flex-sm-row align-items-sm-center justify-content-between gap-2 mb-0");
        alert.Attributes["role"] = "alert";

        var text = new TagBuilder("div");

        var title = new TagBuilder("div");
        title.AddCssClass("fw-semibold");
        title.InnerHtml.Append("Could not load the data.");
        text.InnerHtml.AppendHtml(title);

        var detail = new TagBuilder("div");
        detail.AddCssClass("small");
        detail.InnerHtml.Append(message);
        text.InnerHtml.AppendHtml(detail);
        alert.InnerHtml.AppendHtml(text);

        var retry = new TagBuilder("button");
        retry.AddCssClass("btn btn-sm btn-outline-danger flex-shrink-0");
        retry.Attributes["type"] = "button";
        retry.Attributes["data-dt-refresh"] = string.Empty;
        retry.InnerHtml.Append("Retry");
        alert.InnerHtml.AppendHtml(retry);

        return alert;
    }
}

internal enum DataTablePass
{
    Register,
    Row,
}

/// <summary>The cells one row pass produced, plus how many <c>&lt;dt-column&gt;</c> tags executed.</summary>
internal sealed record DataTableRowCells(
    IReadOnlyList<IHtmlContent> Cells,
    int DeclaredColumns);

internal sealed class DataTableDefinition
{
    // Visibility of every <dt-column> tag, in declaration order, decided once at registration
    // (permission checks are not repeated per row).
    private readonly List<bool> _declaredVisibility = [];

    private List<IHtmlContent>? _cells;

    private int _rowOrdinal;

    public DataTablePass Pass { get; private set; } = DataTablePass.Register;

    public object? CurrentRecord { get; private set; }

    public List<DataTableColumnDefinition> Columns { get; } = [];

    public List<DataTableActionDefinition> Actions { get; } = [];

    public IHtmlContent? Filters { get; set; }

    public int DeclaredColumnCount => _declaredVisibility.Count;

    /// <summary>Registration pass: records one <c>&lt;dt-column&gt;</c> tag and whether it is shown.</summary>
    public void DeclareColumn(bool visible) => _declaredVisibility.Add(visible);

    /// <summary>
    /// Row pass: whether the next <c>&lt;dt-column&gt;</c> tag (by declaration order) is shown. A tag
    /// beyond the registered ones counts as visible, so the row's cell count exposes the mismatch.
    /// </summary>
    public bool NextColumnIsVisible()
    {
        var ordinal = _rowOrdinal++;

        return ordinal >= _declaredVisibility.Count || _declaredVisibility[ordinal];
    }

    public void BeginRow(object? record)
    {
        Pass = DataTablePass.Row;
        CurrentRecord = record;
        _cells = [];
        _rowOrdinal = 0;
    }

    public void AddCell(IHtmlContent content) => _cells?.Add(content);

    public DataTableRowCells EndRow()
    {
        var cells = _cells ?? [];
        _cells = null;

        return new DataTableRowCells(
            cells,
            _rowOrdinal);
    }
}

internal sealed record DataTableColumnDefinition(
    string Key,
    string Header,
    string? Field,
    string? Format,
    string? Css,
    bool Sortable,
    bool Hideable,
    bool IsActions,
    bool Export);

internal sealed record DataTableActionDefinition(
    string Href,
    string? Icon,
    string Variant,
    IHtmlContent Label);

/// <summary>
/// A column of the parent <c>&lt;data-table&gt;</c>. <c>key</c> identifies it (sort key, column
/// toggle); <c>field</c> renders a property, otherwise the child content is the cell template.
/// <c>actions="true"</c> marks the right-aligned, unlabeled row-actions column. <c>permission</c>
/// drops the whole column for users lacking it (checked once, at registration). Attributes are
/// read at registration only and must not reference the current row.
/// </summary>
[HtmlTargetElement("dt-column", ParentTag = "data-table")]
public sealed class DataTableColumnTagHelper(
    IPermissionChecker permissionChecker)
    : TagHelper
{
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public string Key { get; set; } = string.Empty;

    public string Header { get; set; } = string.Empty;

    public string? Field { get; set; }

    /// <summary>.NET format string for <c>field</c> values (numbers default to <c>#,##0.00</c>, integers to <c>#,##0</c>).</summary>
    public string? Format { get; set; }

    /// <summary>Classes applied to both the header and the cells.</summary>
    public string? Css { get; set; }

    public bool Sortable { get; set; }

    public bool? Hideable { get; set; }

    public bool Actions { get; set; }

    public bool? Export { get; set; }

    /// <summary>Comma-separated permissions; the column is shown when the user holds any of them.</summary>
    public string? Permission { get; set; }

    public override async Task ProcessAsync(
        TagHelperContext context,
        TagHelperOutput output)
    {
        output.SuppressOutput();

        if (!context.Items.TryGetValue(typeof(DataTableDefinition), out var value)
            || value is not DataTableDefinition table)
        {
            return;
        }

        if (table.Pass == DataTablePass.Register)
        {
            var visible = IsVisible();
            table.DeclareColumn(visible);

            if (!visible)
            {
                return;
            }

            table.Columns.Add(new DataTableColumnDefinition(
                string.IsNullOrEmpty(Key) ? $"col{table.Columns.Count}" : Key,
                Header,
                Field,
                Format,
                Css,
                Sortable && !Actions,
                Hideable ?? !Actions,
                Actions,
                Export ?? !Actions));

            return;
        }

        if (!table.NextColumnIsVisible())
        {
            return;
        }

        var cell = Field is null
            ? await output.GetChildContentAsync(useCachedResult: false)
            : DataTableCellFormatter.Format(
                table.CurrentRecord,
                Field,
                Format);

        table.AddCell(cell);
    }

    private bool IsVisible()
    {
        return string.IsNullOrEmpty(Permission)
            || permissionChecker.HasAnyPermission(
                ViewContext.HttpContext.User,
                Permission.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}

/// <summary>
/// A toolbar link button of the parent <c>&lt;data-table&gt;</c> (e.g. "Create user"), hidden when the
/// user lacks <c>permission</c>. Rendered from the registration pass only (no current row).
/// </summary>
[HtmlTargetElement("dt-action", ParentTag = "data-table")]
public sealed class DataTableActionTagHelper(
    IPermissionChecker permissionChecker)
    : TagHelper
{
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public string Href { get; set; } = "#";

    public string? Icon { get; set; }

    public string Variant { get; set; } = "primary";

    public string? Permission { get; set; }

    public override async Task ProcessAsync(
        TagHelperContext context,
        TagHelperOutput output)
    {
        if (context.Items.TryGetValue(typeof(DataTableDefinition), out var value)
            && value is DataTableDefinition { Pass: DataTablePass.Register } table
            && (string.IsNullOrEmpty(Permission)
                || permissionChecker.HasAnyPermission(
                    ViewContext.HttpContext.User,
                    Permission.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))))
        {
            table.Actions.Add(new DataTableActionDefinition(
                Href,
                Icon,
                Variant,
                await output.GetChildContentAsync()));
        }

        output.SuppressOutput();
    }
}

/// <summary>
/// Custom filter controls shown next to the search box. Every named <c>input</c>/<c>select</c> inside
/// becomes a query-string value of the table (selects apply on change, text inputs on change/Enter),
/// so the handler binds them like any other query parameter. Rendered from the registration pass only.
/// </summary>
[HtmlTargetElement("dt-filters", ParentTag = "data-table")]
public sealed class DataTableFiltersTagHelper : TagHelper
{
    public override async Task ProcessAsync(
        TagHelperContext context,
        TagHelperOutput output)
    {
        if (context.Items.TryGetValue(typeof(DataTableDefinition), out var value)
            && value is DataTableDefinition { Pass: DataTablePass.Register } table)
        {
            table.Filters = await output.GetChildContentAsync();
        }

        output.SuppressOutput();
    }
}

/// <summary>Formats a <c>field</c> cell by value type.</summary>
internal static class DataTableCellFormatter
{
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> Properties = new();

    public static IHtmlContent Format(
        object? record,
        string field,
        string? format)
    {
        var value = Read(
            record,
            field);

        return value switch
        {
            null => HtmlString.Empty,
            DateTimeOffset dateTimeOffset => LocalDateTimeTagHelper.Render(dateTimeOffset),
            DateTime dateTime => LocalDateTimeTagHelper.Render(new DateTimeOffset(
                dateTime.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                    : dateTime)),
            Enum => new StringHtmlContent(value.ToString() ?? string.Empty),
            decimal or double or float => Numeric(((IFormattable)value).ToString(
                format ?? NumberTagHelper.DefaultFormat,
                CultureInfo.InvariantCulture)),
            int or long or short or byte => Numeric(((IFormattable)value).ToString(
                format ?? "#,##0",
                CultureInfo.InvariantCulture)),
            IFormattable formattable when format is not null => new StringHtmlContent(formattable.ToString(
                format,
                CultureInfo.InvariantCulture)),
            _ => new StringHtmlContent(Convert.ToString(
                value,
                CultureInfo.InvariantCulture) ?? string.Empty),
        };
    }

    private static IHtmlContent Numeric(string text)
    {
        var span = new TagBuilder("span");
        span.AddCssClass("numeric");
        span.InnerHtml.Append(text);
        return span;
    }

    /// <summary>Reads a (dotted) property path by reflection, caching the accessors per type.</summary>
    private static object? Read(
        object? target,
        string path)
    {
        foreach (var segment in path.Split('.'))
        {
            if (target is null)
            {
                return null;
            }

            var property = Properties.GetOrAdd(
                (target.GetType(), segment),
                key => FindProperty(
                    key.Item1,
                    key.Item2));

            target = property?.GetValue(target);
        }

        return target;
    }

    /// <summary>
    /// Exact-case match first; otherwise a case-insensitive match only when it is unique —
    /// <see cref="Type.GetProperty(string, BindingFlags)"/> with <c>IgnoreCase</c> throws
    /// <see cref="AmbiguousMatchException"/> when two properties differ only by case (or a derived
    /// type hides a base property).
    /// </summary>
    private static PropertyInfo? FindProperty(
        Type type,
        string name)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetIndexParameters().Length == 0)
            .ToList();

        var exact = properties
            .Where(property => string.Equals(property.Name, name, StringComparison.Ordinal))
            .ToList();

        if (exact.Count > 0)
        {
            // Hidden base properties show up too; the most derived declaration wins.
            return exact.MaxBy(property => Depth(property.DeclaringType));
        }

        var matches = properties
            .Where(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return matches.Count == 1 ? matches[0] : null;
    }

    private static int Depth(Type? type)
    {
        var depth = 0;

        for (var current = type; current is not null; current = current.BaseType)
        {
            depth++;
        }

        return depth;
    }
}
