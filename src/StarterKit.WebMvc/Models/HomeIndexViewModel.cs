namespace StarterKit.WebMvc.Models;

public sealed record HomeIndexViewModel(
    string DisplayName,
    string? UserName,
    IReadOnlyList<string> Roles,
    int PermissionCount,
    int? UnreadNotificationCount);
