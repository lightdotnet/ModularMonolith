namespace StarterKit.Identity.Contracts.Users;

/// <summary>
/// In-process cross-module notification raised by Identity immediately after a user is
/// created and the change is committed. Consuming modules subscribe with an
/// <c>INotificationHandler&lt;T&gt;</c> to react to onboarding (for example, sending a
/// welcome email). Delivery is best-effort-immediate.
/// </summary>
public sealed record UserCreatedIntegrationEvent(
    string UserId,
    string? UserName,
    string? Email) : INotification;
