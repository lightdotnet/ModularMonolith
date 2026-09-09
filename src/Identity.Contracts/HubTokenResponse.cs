namespace StarterKit.Identity.Contracts;

/// <summary>
/// Short-lived, hub-audience-only token the browser uses for the SignalR handshake.
/// </summary>
public record HubTokenResponse(
    string AccessToken,
    int ExpiresIn);
