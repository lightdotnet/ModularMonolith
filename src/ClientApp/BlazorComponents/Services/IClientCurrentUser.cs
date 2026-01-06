namespace Monolith.Blazor.Services;

public interface IClientCurrentUser : ICurrentUser
{
    string? FirstName { get; }

    string? LastName { get; }

    string? FullName { get; }

    string? PhoneNumber { get; }

    string? Email { get; }
}
