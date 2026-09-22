using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Currency.Tests.TestSupport;

/// <summary>
/// Simulates a concurrent writer: runs an action once, right before the first save, so a check-then-insert
/// can be beaten to the insert by "another request".
/// </summary>
public sealed class RaceInterceptor(Func<Task> competingWrite) : SaveChangesInterceptor
{
    private bool _fired;

    /// <summary>The write only fires once armed, so fixture seeding saves do not trigger it.</summary>
    public bool Armed { get; set; }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (Armed && !_fired)
        {
            _fired = true;
            await competingWrite();
        }

        return result;
    }
}
