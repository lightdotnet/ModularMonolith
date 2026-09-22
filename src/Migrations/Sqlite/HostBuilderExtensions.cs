using Light.Serilog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Sqlite;

public static class Host
{
    public static IConfiguration Configuration { get; } = new ConfigurationBuilder().SetConfiguration().Build();

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args);

        host
            .ConfigureAppConfiguration((context, builder) =>
            {
                builder.AddConfiguration(Configuration);
            })
            .ConfigureServices(services =>
            {
                // inject services here...
                services.AddMigrator(Configuration);
            });

        // Serilog
        host.ConfigureSerilog();

        return host;
    }

    private static IConfigurationBuilder SetConfiguration(this IConfigurationBuilder configurationbuilder)
    {
        configurationbuilder
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Staging"}.json", optional: true);

        // load *.json file from folder Configurations
        var path = Path.Combine(Directory.GetCurrentDirectory());
        var dinfo = new DirectoryInfo(path);
        FileInfo[] files = dinfo.GetFiles("*.json");

        foreach (var file in files)
        {
            configurationbuilder
                .AddJsonFile(Path.Combine(path, file.Name), optional: false, reloadOnChange: true);
        }

        configurationbuilder.AddEnvironmentVariables().Build();

        return configurationbuilder;
    }
}
