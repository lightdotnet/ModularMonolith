using Serilog;

namespace StarterKit.Infrastructure;

public class AppLogging
{
    private static Serilog.Core.Logger? _logger;

    public static Serilog.Core.Logger Logger
    {
        get
        {
            if (_logger is null)
            {
                _logger ??= new LoggerConfiguration()
                    .WriteTo.Async(c => c.Console())
                    .WriteTo.Async(c => c.File(@"logs\application-startup.txt",
                        shared: true,
                        rollOnFileSizeLimit: true,
                        fileSizeLimitBytes: 52428800)) // 50mb
                    .CreateLogger();

                _logger.Warning("Static Logger initialized");
            }

            return _logger;
        }
    }

    public static void Information(string message, params object[] values)
    {
        Logger.Information(message, values);
    }

    public static void Warning(string message, params object[] values)
    {
        Logger.Warning(message, values);
    }

    public static void CloseAndFlush()
    {
        _logger?.Dispose();
        _logger = null;
    }
}
