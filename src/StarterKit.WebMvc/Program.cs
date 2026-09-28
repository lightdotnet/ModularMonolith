using Light.Serilog;
using Serilog;
using StarterKit.WebMvc.Infrastructure;

// StarterKit.WebMvc is a separate front-end host: an HTTP client of StarterKit.WebApi (like
// clients/admin), serving MVC views + Razor Pages behind its own cookie session.
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.ConfigureSerilog();

    builder.Services
        .AddWebMvcServices(builder.Configuration)
        .AddWebMvcAuthentication(
            builder.Configuration,
            builder.Environment);

    var app = builder.Build();

    // First: client IP/scheme from trusted proxies feed HTTPS redirection, Secure cookies,
    // redirect URIs and the per-IP sign-in rate limit.
    app.UseForwardedHeaders();
    app.UseSecurityHeaders();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseStatusCodePagesWithReExecute(
        "/Error",
        "?statusCode={0}");

    app.UseStaticFiles();
    app.UseRouting();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseApiAuthorizationFailureHandling();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapRazorPages();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(
        ex,
        "StarterKit.WebMvc start-up failed.");
}
finally
{
    Log.CloseAndFlush();
}
