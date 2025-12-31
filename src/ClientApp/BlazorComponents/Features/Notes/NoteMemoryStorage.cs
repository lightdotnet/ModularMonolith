using Light.Contracts;
using Monolith.Blazor.Services;
using System.Collections.Concurrent;

namespace Monolith.Blazor.Features.Notes;

public class NoteMemoryStorage(IClientCurrentUser currentUser)
{
    private readonly ConcurrentDictionary<string, string> _notes = new();

    private readonly string _userId = currentUser.UserId ?? throw new InvalidOperationException("User is not authenticated.");

    public Task<string?> GetAsync()
    {
        _notes.TryGetValue(_userId, out var data);
        return Task.FromResult(data);
    }

    public Task<Result> SaveAsync(string data)
    {
        _notes[_userId] = data;
        return Task.FromResult(Result.Success());
    }

    public Task RemoveAsync()
    {
        _notes.TryRemove(_userId, out _);
        return Task.CompletedTask;
    }
}
