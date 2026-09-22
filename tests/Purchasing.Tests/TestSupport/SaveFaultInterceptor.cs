using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Purchasing.Tests.TestSupport;

/// <summary>
/// Lets a test simulate a concurrent writer: once <see cref="Arm"/>ed, before a chosen
/// <c>SaveChangesAsync</c> it can run an action (e.g. persist a competing row through a second
/// context) and/or fail the save with a <see cref="DbUpdateConcurrencyException"/> (for the next
/// <c>failTimes</c> saves). Saves before arming (seeding) are never affected.
/// </summary>
public sealed class SaveFaultInterceptor : SaveChangesInterceptor
{
    private bool _armed;
    private int _skip;
    private int _failRemaining;

    public int SavesIntercepted { get; private set; }

    /// <summary>Runs before the first targeted save (once).</summary>
    public Func<Task>? BeforeSave { get; private set; }

    /// <param name="skipSaves">Number of saves after arming that pass through untouched.</param>
    /// <param name="failTimes">How many targeted saves throw a concurrency exception.</param>
    public void Arm(
        int skipSaves = 0,
        Func<Task>? beforeSave = null,
        int failTimes = 0)
    {
        _armed = true;
        _skip = skipSaves;
        _failRemaining = failTimes;
        BeforeSave = beforeSave;
        SavesIntercepted = 0;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (!_armed)
            return result;

        if (_skip-- > 0)
            return result;

        SavesIntercepted++;

        if (BeforeSave is not null)
        {
            var action = BeforeSave;
            BeforeSave = null;

            // The competing write goes through its own context; do not intercept it recursively.
            _armed = false;
            await action();
            _armed = true;
        }

        if (_failRemaining > 0)
        {
            _failRemaining--;

            if (_failRemaining == 0 && BeforeSave is null)
            {
                // Last scripted failure: further saves pass through.
                _armed = false;
            }

            throw new DbUpdateConcurrencyException("Simulated concurrent modification.");
        }

        _armed = false;

        return result;
    }
}
