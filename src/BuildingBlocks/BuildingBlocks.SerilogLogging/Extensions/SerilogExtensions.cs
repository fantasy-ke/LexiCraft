using Microsoft.AspNetCore.Builder;
using BuildingBlocks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace BuildingBlocks.SerilogLogging.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder, 
        Action<SerilogOptions>? configureOptions = null)
    {
        builder.Services.AddConfigurationOptions("SerilogLogging", configureOptions);

        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            var options = services.GetRequiredService<IOptions<SerilogOptions>>().Value;

            // Minimum Level
            loggerConfiguration.MinimumLevel.Information(); // Default
            
            // Overrides
            foreach (var overrideConfig in options.MinimumLevelOverrides)
            {
                if (Enum.TryParse<LogEventLevel>(overrideConfig.Value, true, out var level))
                {
                    loggerConfiguration.MinimumLevel.Override(overrideConfig.Key, level);
                }
            }

            // Enrichers
            var appName = options.ApplicationName ?? context.HostingEnvironment.ApplicationName;
            loggerConfiguration
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("ApplicationName", appName);

            // Helper to configure sink with optional Async wrapper
            void ConfigureSink(Action<LoggerSinkConfiguration> configureSink)
            {
                if (options.EnableAsync)
                {
                    loggerConfiguration.WriteTo.Async(configureSink);
                }
                else
                {
                    configureSink(loggerConfiguration.WriteTo);
                }
            }

            // Sinks
            if (options.EnableConsole)
            {
                ConfigureSink(a => a.Console(outputTemplate: options.LogTemplate));
            }

            if (options.EnableFileLogging)
            {
                var logLevels = new[]
                {
                    (Level: LogEventLevel.Information, Suffix: "Info"),
                    (Level: LogEventLevel.Warning, Suffix: "Warn"),
                    (Level: LogEventLevel.Error, Suffix: "Error")
                };

                foreach (var (level, suffix) in logLevels)
                {
                    ConfigureSink(a => a.File(
                        path: Path.Combine(options.LogPath, $"{suffix}.txt"),
                        restrictedToMinimumLevel: level,
                        rollingInterval: RollingInterval.Day,
                        rollOnFileSizeLimit: true,
                        fileSizeLimitBytes: options.FileSizeLimitBytes,
                        retainedFileCountLimit: options.RetainedFileCountLimit,
                        outputTemplate: options.LogTemplate
                    ));
                }
            }

            // Seq
            if (options.Seq.Enabled)
            {
                ConfigureSink(a => a.Seq(
                    serverUrl: options.Seq.ServerUrl,
                    apiKey: options.Seq.ApiKey
                ));
            }
        });

        return builder;
    }
}
