using System.Collections.Concurrent;
using System.Diagnostics;
using BuildingBlocks.MongoDB.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.MongoDB.Performance;

public class MongoPerformanceMonitor : IMongoPerformanceMonitor
{
    private const int MaxMetricCount = 10_000;
    private static readonly TimeSpan SlowOperationThreshold = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan VerySlowOperationThreshold = TimeSpan.FromSeconds(1);

    private readonly bool _enabled;
    private readonly ILogger<MongoPerformanceMonitor> _logger;
    private readonly ConcurrentQueue<OperationMetric> _metrics = new();
    private readonly object _metricTrimLock = new();
    private int _metricCount;

    public MongoPerformanceMonitor(
        ILogger<MongoPerformanceMonitor> logger,
        IOptions<MongoOptions> options)
    {
        _logger = logger;
        _enabled = options.Value.EnablePerformanceMonitoring;
    }

    public IDisposable StartOperation(string operationName, string collectionName)
    {
        return _enabled
            ? new OperationTimer(operationName, collectionName, this)
            : EmptyDisposable.Instance;
    }

    public Task<PerformanceMetrics> GetMetricsAsync(TimeSpan? period = null)
    {
        var selectedPeriod = period ?? TimeSpan.FromMinutes(5);
        if (selectedPeriod <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(period), "Metrics period must be greater than zero.");

        if (!_enabled) return Task.FromResult(new PerformanceMetrics());

        var cutoffTime = DateTime.UtcNow - selectedPeriod;
        var validMetrics = _metrics.ToArray().Where(metric => metric.Timestamp >= cutoffTime).ToArray();
        if (validMetrics.Length == 0) return Task.FromResult(new PerformanceMetrics());

        return Task.FromResult(new PerformanceMetrics
        {
            TotalOperations = validMetrics.Length,
            AverageResponseTime = TimeSpan.FromMilliseconds(
                validMetrics.Average(metric => metric.Duration.TotalMilliseconds)),
            MaxResponseTime = validMetrics.Max(metric => metric.Duration),
            MinResponseTime = validMetrics.Min(metric => metric.Duration),
            OperationsPerSecond = validMetrics.Length / selectedPeriod.TotalSeconds,
            SlowOperations = validMetrics.Count(metric => metric.Duration > SlowOperationThreshold),
            OperationsByCollection = validMetrics
                .GroupBy(metric => metric.CollectionName)
                .ToDictionary(group => group.Key, group => group.Count()),
            OperationsByType = validMetrics
                .GroupBy(metric => metric.OperationName)
                .ToDictionary(group => group.Key, group => group.Count())
        });
    }

    internal void RecordOperation(string operationName, string collectionName, TimeSpan duration)
    {
        if (!_enabled) return;

        _metrics.Enqueue(new OperationMetric
        {
            OperationName = operationName,
            CollectionName = collectionName,
            Duration = duration,
            Timestamp = DateTime.UtcNow
        });

        if (Interlocked.Increment(ref _metricCount) > MaxMetricCount)
        {
            lock (_metricTrimLock)
            {
                while (Volatile.Read(ref _metricCount) > MaxMetricCount && _metrics.TryDequeue(out _))
                    Interlocked.Decrement(ref _metricCount);
            }
        }

        if (duration > VerySlowOperationThreshold)
            _logger.LogError(
                "Very slow MongoDB operation: {Operation} on {Collection} took {Duration}ms",
                operationName,
                collectionName,
                duration.TotalMilliseconds);
        else if (duration > SlowOperationThreshold)
            _logger.LogWarning(
                "Slow MongoDB operation detected: {Operation} on {Collection} took {Duration}ms",
                operationName,
                collectionName,
                duration.TotalMilliseconds);
    }


    private sealed class OperationTimer(
        string operationName,
        string collectionName,
        MongoPerformanceMonitor monitor) : IDisposable
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

            _stopwatch.Stop();
            monitor.RecordOperation(operationName, collectionName, _stopwatch.Elapsed);
        }
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}