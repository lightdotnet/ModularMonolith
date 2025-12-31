using Microsoft.JSInterop;

namespace Monolith.Blazor.Extensions;

public static class NotificationExtensions
{
    public const string NOTI_PLAYER_NAME = "notiPlayer";

    public static async Task PlaySound(this IJSRuntime jsRuntime, string playerName) =>
        await jsRuntime.InvokeVoidAsync("playAudio", playerName);

    public static async Task PauseSound(this IJSRuntime jsRuntime, string playerName) =>
        await jsRuntime.InvokeVoidAsync("pauseAudio", playerName);

    public static Task PlayNotiSound(this IJSRuntime jsRuntime) =>
        jsRuntime.PlaySound(NOTI_PLAYER_NAME);

    public static Task PauseNotiSound(this IJSRuntime jsRuntime) =>
        jsRuntime.PauseSound(NOTI_PLAYER_NAME);

    public static async Task RequestPermission(this IJSRuntime jsRuntime) =>
        await jsRuntime.InvokeVoidAsync("requestNotificationPermission");

    public static async Task ShowNotification(this IJSRuntime jsRuntime, string subject, string? content)
    {
        var options = new { body = content };
        await jsRuntime.InvokeVoidAsync("notify", subject, options);
    }

    public static async Task SetAppBadge(this IJSRuntime jsRuntime) =>
        await jsRuntime.InvokeVoidAsync("setAppBadge");

    public static async Task ClearAppBadge(this IJSRuntime jsRuntime) =>
        await jsRuntime.InvokeVoidAsync("clearAppBadge");
}
