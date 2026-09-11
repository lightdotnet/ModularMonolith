using System.Linq.Expressions;

namespace StarterKit.Persistence.Repositories
{
    public interface IDynamicsDbCache<T>
        where T : class
    {
        Task<IReadOnlyList<T>> RefreshCacheAsync(CancellationToken cancellationToken = default);

        Task RemoveCacheAsync(CancellationToken cancellationToken = default);

        IQueryable<T> Where(Expression<Func<T, bool>> expression);

        Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
