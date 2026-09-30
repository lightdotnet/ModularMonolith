using Light.Serilog;
using Serilog;
using Spectre.Console;
using StarterKit.WebApi;
using StarterKit.Infrastructure;

AnsiConsole.Write(new FigletText("Starter API").Color(Color.Blue));

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog stays the logging pipeline. The default Microsoft.Extensions.Logging providers
    // are cleared so Serilog's own console sink is the only console output; writeToProviders
    // then forwards Serilog events to the providers still registered - the OpenTelemetry
    // logger provider added by AddServiceDefaults, which feeds the Aspire dashboard.
    builder.Logging.ClearProviders();

    // Aspire service defaults: OpenTelemetry (logs, metrics, traces), service discovery,
    // HTTP client resilience and the "self" liveness health check.
    builder.AddServiceDefaults();

    builder.Host.UseSerilog(
        SerilogConfigurationExtensions.Configure,
        writeToProviders: true);

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

    // Health endpoints: /hc (mapped in ConfigurePipelines) is the deployment health endpoint,
    // available in every environment; /health and /alive are the Aspire dev endpoints,
    // mapped by MapDefaultEndpoints in Development only.
    app.MapDefaultEndpoints();

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
