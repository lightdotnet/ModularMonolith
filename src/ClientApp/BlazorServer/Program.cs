using Monolith.Blazor;
using Monolith.Blazor.Components;

var builder = WebApplication.CreateBuilder(args);

// modify the default antiforgery cookie name
UnsafeAccessorClassAntiforgeryOptions.GetUnsafeStaticFieldDefaultCookiePrefix(new()) = ".anti-forgery.";

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();

builder.Services.AddWebServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Map to "/" for fix error always show NotFound page
app.UseStatusCodePagesWithReExecute("/", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

app.UseAntiforgery();

app.AddWebPipelines();

app.MapStaticAssets();

app.MapRazorPages();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorComponents).Assembly);

app.Run();
