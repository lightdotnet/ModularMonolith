namespace Monolith.Identity.Extensions;

public class CollectionSyncExtensions
{
    public static async Task SyncCollectionAsync<T>(
        IEnumerable<T> current,
        IEnumerable<T> target,
        Func<T, Task> removeAsync,
        Func<T, Task> addAsync,
        IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;

        var currentSet = current.ToHashSet(comparer);
        var targetSet = target.ToHashSet(comparer);

        var toRemove = currentSet.Except(targetSet, comparer);
        foreach (var item in toRemove)
            await removeAsync(item).ConfigureAwait(false);

        var toAdd = targetSet.Except(currentSet, comparer);
        foreach (var item in toAdd)
            await addAsync(item).ConfigureAwait(false);
    }

    public static async Task SyncCollectionAsync<T>(
        IEnumerable<T> current,
        IEnumerable<T> target,
        Func<IEnumerable<T>, Task> removeAsync,
        Func<IEnumerable<T>, Task> addAsync,
        IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;

        var currentSet = current.ToHashSet(comparer);
        var targetSet = target.ToHashSet(comparer);

        var toRemove = currentSet.Except(targetSet, comparer).ToList();
        if (toRemove.Count > 0)
            await removeAsync(toRemove).ConfigureAwait(false);

        var toAdd = targetSet.Except(currentSet, comparer).ToList();
        if (toAdd.Count > 0)
            await addAsync(toAdd).ConfigureAwait(false);
    }
}
