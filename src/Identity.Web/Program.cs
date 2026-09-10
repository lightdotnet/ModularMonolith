using StarterKit.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// The standalone host serves only cookie-authenticated Razor Pages — no Bearer scheme,
// no policy scheme, no /api or SignalR hub handling (that is AddApiAuthentication,
// which only the co-host StarterKit.WebApi calls).
//
// The co-host gets its platform + mediator services from a monolith-wide module scan
// plus ConfigureExtensions; the standalone host composes the Identity-only equivalent
// in AddIdentityWebHost. Both hosts now run the same mediator behaviors (logging +
// validation), so a future Razor page that dispatches a mediator command behaves
// identically on either host — the only difference is scan scope: the standalone host
// sees the Identity assembly alone, so it still has NO cross-module notification
// handlers and ExternalUserProvisionedIntegrationEvent stays intentionally unhandled
// here (the JIT welcome email is sent only under the co-host).
builder.Services.AddIdentityWebHost(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseIdentityWeb();

app.Run();
