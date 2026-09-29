using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StarterKit.Persistence.Extensions;

public static class IndexBuilderExtensions
{
    /// <summary>
    /// Applies a filtered-index predicate with provider-appropriate quoting: <paramref name="filter"/>
    /// (bracket-quoted identifiers, valid on SQL Server and Sqlite) everywhere except Npgsql, which
    /// gets <paramref name="postgresFilter"/> (double-quoted identifiers, real boolean literals).
    /// </summary>
    public static IndexBuilder<TEntity> HasProviderFilter<TEntity>(
        this IndexBuilder<TEntity> builder,
        DatabaseFacade database,
        string filter,
        string postgresFilter)
    {
        return builder.HasFilter(database.IsNpgsql() ? postgresFilter : filter);
    }
}
