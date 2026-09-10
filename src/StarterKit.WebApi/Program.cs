using Light.Serilog;
using Serilog;
using Spectre.Console;
using StarterKit.Infrastructure;
using StarterKit.WebApi;

AnsiConsole.Write(new FigletText("Starter API").Color(Color.Blue));

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.ConfigureSerilog();

    // Add static Configuration to the container.

    // Add services to the container.
    builder.Services.ConfigureServices(builder.Configuration);

    builder.Services
        .AddLowercaseControllers()
        .AddDefaultJsonOptions()
        .AddInvalidModelStateHandler();

    var app = builder.Build();

    // Configure the HTTP request pipeline.

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.ConfigurePipelines();

    app.UseWebSockets();

    app.MapEndpoints(builder.Configuration.GetValue<bool>("AllowAnonymous"));

    app.Run();
}
catch (Exception ex) when (!ex.GetType().Name.Equals("StopTheHostException", StringComparison.Ordinal))
{
    AppLogging.Logger.Fatal("Unhandled exception: {ex}", ex);
    AppLogging.Logger.Error("Application start-up failed: {ex}", ex);
}
finally
{
    Log.Information("Shut down complete.");
    Log.CloseAndFlush();
    AppLogging.CloseAndFlush();
}