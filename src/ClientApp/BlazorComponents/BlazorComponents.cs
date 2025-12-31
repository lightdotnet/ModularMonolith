using Microsoft.Extensions.DependencyInjection;

namespace Monolith.Blazor;

public static class BlazorComponents
{
    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        services.AddSingleton<Features.Notes.NoteMemoryStorage>();

        return services;
    }
}