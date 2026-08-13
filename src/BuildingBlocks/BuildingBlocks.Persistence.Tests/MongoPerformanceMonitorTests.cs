using BuildingBlocks.MongoDB.Configuration;
using BuildingBlocks.MongoDB.Performance;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Persistence.Tests;

public class MongoPerformanceMonitorTests
{
    [Fact]
    public async Task Disabled_monitor_does_not_record_operations()
    {
        var monitor = CreateMonitor(false);

        monitor.StartOperation("Find", "items").Dispose();
        var metrics = await monitor.GetMetricsAsync(TimeSpan.FromMinutes(1));

        Assert.Equal(0, metrics.TotalOperations);
    }

    [Fact]
    public async Task Monitor_keeps_a_bounded_number_of_metrics()
    {
        var monitor = CreateMonitor(true);

        for (var index = 0; index < 10_005; index++)
            monitor.StartOperation("Find", "items").Dispose();

        var metrics = await monitor.GetMetricsAsync(TimeSpan.FromHours(1));

        Assert.Equal(10_000, metrics.TotalOperations);
    }

    [Fact]
    public async Task Monitor_keeps_exact_bound_under_concurrent_recording()
    {
        var monitor = CreateMonitor(true);

        Parallel.For(0, 20_000, _ => monitor.StartOperation("Find", "items").Dispose());
        var metrics = await monitor.GetMetricsAsync(TimeSpan.FromHours(1));

        Assert.Equal(10_000, metrics.TotalOperations);
    }
    [Fact]
    public void Monitor_rejects_non_positive_period()
    {
        var monitor = CreateMonitor(true);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = monitor.GetMetricsAsync(TimeSpan.Zero);
        });
    }

    private static MongoPerformanceMonitor CreateMonitor(bool enabled)
    {
        return new MongoPerformanceMonitor(
            NullLogger<MongoPerformanceMonitor>.Instance,
            Options.Create(new MongoOptions { EnablePerformanceMonitoring = enabled }));
    }
}
