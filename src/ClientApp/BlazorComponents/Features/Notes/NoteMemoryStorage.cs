using Light.Contracts;
using System.Collections.Concurrent;

namespace Monolith.Blazor.Features.Notes;

public class NoteMemoryStorage
{
    private readonly ConcurrentDictionary<string, string> _notes = new();

    public Task<string?> GetAsync(string userId)
    {
        _notes.TryGetValue(userId, out var data);
        return Task.FromResult(data);
    }

    public Task<Result> SaveAsync(string userId, string data)
    {
        _notes[userId] = data;
        return Task.FromResult(Result.Success());
    }

    public Task RemoveAsync(string userId)
    {
        _notes.TryRemove(userId, out _);
        return Task.CompletedTask;
    }
}
