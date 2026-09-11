using Light.Extensions.DynamicObject;

namespace StarterKit.Persistence.Repositories;

/// <summary>
///     No optimistic concurrency token is enforced here: <see cref="Update"/> reads the current
///     <typeparamref name="TEntity"/> rows, mutates the tracked instances in place, and saves —
///     two concurrent <see cref="Update"/> calls for the same <paramref name="objectName"/> (in
///     <see cref="Update"/>) are last-writer-wins with no conflict detection. This repository has
///     no consumer today; if one is added, configure a concurrency token on <typeparamref name="TEntity"/>
///     (e.g. <c>IsConcurrencyToken()</c> on its <c>LastModified</c> column) in the consuming
///     <typeparamref name="TContext"/>'s model configuration before relying on this under concurrent writes.
/// </summary>
public abstract class DynamicTableRepository<T, TEntity, TContext>(TContext context)
    : IDynamicEntityRepository<T>
    where T : new()
    where TEntity : DynamicEntity
    where TContext : DbContext
{
    public virtual async Task<T?> Get(string objectName)
    {
        var columns = await context.Set<TEntity>()
            .Where(x => x.ObjectName == objectName)
            .AsNoTracking()
            .ToListAsync();

        if (columns.Count == 0)
            return default;

        return DynamicMapper.MapToObject<T, TEntity>(columns);
    }

    public virtual async Task Update(string objectName, T value)
    {
        var columns = DynamicColumnExporter.ConvertToDynamicColumns<T, TEntity>(value, objectName);

        var tableColumns = await context.Set<TEntity>()
            .Where(x => x.ObjectName == objectName)
            .ToListAsync();

        foreach (var column in columns)
        {
            var existingColumn = tableColumns
                .FirstOrDefault(c =>
                    c.ObjectName == column.ObjectName
                    && c.PropName == column.PropName);

            if (existingColumn != null)
            {
                existingColumn.PropType = column.PropType;
                existingColumn.PropValue = column.PropValue;
                existingColumn.LastModified = DateTimeOffset.UtcNow;
            }
            else
            {
                context.Set<TEntity>().Add(column);
            }
        }

        await context.SaveChangesAsync();
    }
}
