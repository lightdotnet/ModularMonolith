using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Transfers.Tests.TestSupport;

/// <summary>
/// Lets a test simulate a concurrent writer: once <see cref="Arm"/>ed, before the chosen
/// <c>SaveChangesAsync</c> it can run an action (e.g. persist a competing row through a second
/// context) and/or fail the save with a <see cref="DbUpdateConcurrencyException"/>. Saves before
/// arming (seeding) are never affected.
/// </summary>
public sealed class SaveFaultInterceptor : SaveChangesInterceptor
{
    private bool _armed;
    private int _skip;

    /// <summary>Runs before the targeted save (once).</summary>
    public Func<Task>? BeforeSave { get; private set; }

    /// <summary>When true the targeted save throws <see cref="DbUpdateConcurrencyException"/> (once).</summary>
    public bool FailWithConcurrency { get; private set; }

    /// <param name="skipSaves">Number of saves after arming that pass through untouched.</param>
    public void Arm(
        int skipSaves = 0,
        Func<Task>? beforeSave = null,
        bool failWithConcurrency = false)
    {
        _armed = true;
        _skip = skipSaves;
        BeforeSave = beforeSave;
        FailWithConcurrency = failWithConcurrency;
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

        _armed = false;

        if (BeforeSave is not null)
            await BeforeSave();

        if (FailWithConcurrency)
            throw new DbUpdateConcurrencyException("Simulated concurrent modification.");

        return result;
    }
}
