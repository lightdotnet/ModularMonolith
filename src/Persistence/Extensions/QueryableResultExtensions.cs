using Light.Contracts;
using Light.EntityFrameworkCore.Extensions;

namespace StarterKit.Persistence.Extensions;

public static class QueryableResultExtensions
{
    public const int MaxPageSize = 100;

    public static async Task<Paged<T>> ToPagedAsync<T>(this IQueryable<T> queryable,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, MaxPageSize);
        var count = await queryable.CountAsync(cancellationToken);
        var items = await queryable.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new Paged<T>(items, pageNumber, pageSize, count);
    }

    public static Task<Paged<T>> ToPagedAsync<T>(this IQueryable<T> queryable, IPage page, CancellationToken cancellationToken = default) =>
        queryable.ToPagedAsync(page.PageNumber, page.PageSize, cancellationToken);

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> queryable,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paged = await queryable.ToPagedAsync(pageNumber, pageSize, cancellationToken);

        return new PagedResult<T>(paged);
    }

    public static Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> queryable,
        IPage page,
        CancellationToken cancellationToken = default) =>
        queryable.ToPagedResultAsync(page.PageNumber, page.PageSize, cancellationToken);

    public static async Task<Result<IEnumerable<T>>> ToListResultAsync<T>(this IQueryable<T> queryable,
        CancellationToken cancellationToken = default)
    {
        var items = await queryable.ToListAsync(cancellationToken);
        return Result<IEnumerable<T>>.Success(items);
    }

    public static async Task<Result<T>> FirstResultAsync<T>(this IQueryable<T> queryable,
        string objectName, object queryValue,
        CancellationToken cancellationToken = default)
    {
        var entry = await queryable.FirstOrDefaultAsync(cancellationToken);

        if (entry == null)
            return NotFound<T>(objectName, queryValue);

        return Result<T>.Success(entry);
    }

    public static async Task<Result<T>> LastResultAsync<T>(this IQueryable<T> queryable,
        string objectName, object queryValue,
        CancellationToken cancellationToken = default)
    {
        var entry = await queryable.LastOrDefaultAsync(cancellationToken);

        if (entry == null)
            return NotFound<T>(objectName, queryValue);

        return Result<T>.Success(entry);
    }

    public static async Task<Result<T>> SingleResultAsync<T>(this IQueryable<T> queryable,
        string objectName, object queryValue,
        CancellationToken cancellationToken = default)
    {
        var entry = await queryable.SingleOrDefaultAsync(cancellationToken);

        if (entry == null)
            return NotFound<T>(objectName, queryValue);

        return Result<T>.Success(entry);
    }

    private static Result<T> NotFound<T>(string objectName, object queryValue)
    {
        return Result<T>.NotFound($"Query object {objectName} by {queryValue} not found");
    }
}
