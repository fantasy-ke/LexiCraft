using BuildingBlocks.Extensions;
using Microsoft.AspNetCore.Builder;
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
                if (Enum.TryParse<LogEventLevel>(overrideConfig.Value, true, out var level))
                    loggerConfiguration.MinimumLevel.Override(overrideConfig.Key, level);

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
                    loggerConfiguration.WriteTo.Async(configureSink);
                else
                    configureSink(loggerConfiguration.WriteTo);
            }

            // Sinks
            if (options.EnableConsole) ConfigureSink(a => a.Console(outputTemplate: options.LogTemplate));

            if (options.EnableFileLogging)
            {
                var logLevels = new[]
                {
                    (Suffix: "Info", Filter: (Func<LogEvent, bool>)(e => e.Level == LogEventLevel.Information)),
                    (Suffix: "Warn", Filter: (Func<LogEvent, bool>)(e => e.Level == LogEventLevel.Warning)),
                    (Suffix: "Error",
                        Filter: (Func<LogEvent, bool>)(e => e.Level is LogEventLevel.Error or LogEventLevel.Fatal))
                };

                foreach (var (suffix, filter) in logLevels)
                    ConfigureSink(a => a.Logger(lc => lc
                        .Filter.ByIncludingOnly(filter)
                        .WriteTo.File(
                            Path.Combine(options.LogPath, $"{suffix}.txt"),
                            rollingInterval: RollingInterval.Day,
                            rollOnFileSizeLimit: true,
                            fileSizeLimitBytes: options.FileSizeLimitBytes,
                            retainedFileCountLimit: options.RetainedFileCountLimit,
                            outputTemplate: options.LogTemplate
                        )));
            }

            // Seq
            if (options.Seq.Enabled)
                ConfigureSink(a => a.Seq(
                    options.Seq.ServerUrl,
                    apiKey: options.Seq.ApiKey
                ));
        });

        return builder;
    }
}