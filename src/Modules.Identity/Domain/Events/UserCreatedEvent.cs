namespace Monolith.Identity.Domain.Events;

public record UserCreatedEvent(string UserId, string? UserName, string? Email) : DomainEvent;
