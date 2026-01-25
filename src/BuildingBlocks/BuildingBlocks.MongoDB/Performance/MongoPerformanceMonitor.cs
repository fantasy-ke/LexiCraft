using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.MongoDB.Performance;

/// <summary>
///     MongoDB 性能监控实现
/// </summary>
public class MongoPerformanceMonitor : IMongoPerformanceMonitor
{
    private readonly object _lockObject = new();
    private readonly ILogger<MongoPerformanceMonitor> _logger;
    private readonly ConcurrentQueue<OperationMetric> _metrics = new();

    public MongoPerformanceMonitor(ILogger<MongoPerformanceMonitor> logger)
    {
        _logger = logger;
    }

    public IDisposable StartOperation(string operationName, string collectionName)
    {
        return new OperationTimer(operationName, collectionName, this, _logger);
    }

    public Task<PerformanceMetrics> GetMetricsAsync(TimeSpan? period = null)
    {
        var cutoffTime = DateTime.UtcNow - (period ?? TimeSpan.FromMinutes(5));

        lock (_lockObject)
        {
            // 清理旧指标
            var validMetrics = new List<OperationMetric>();
            while (_metrics.TryDequeue(out var metric))
                if (metric.Timestamp >= cutoffTime)
                    validMetrics.Add(metric);

            // 重新添加有效指标
            foreach (var metric in validMetrics) _metrics.Enqueue(metric);

            if (!validMetrics.Any()) return Task.FromResult(new PerformanceMetrics());

            var metrics = new PerformanceMetrics
            {
                TotalOperations = validMetrics.Count,
                AverageResponseTime =
                    TimeSpan.FromMilliseconds(validMetrics.Average(m => m.Duration.TotalMilliseconds)),
                MaxResponseTime = validMetrics.Max(m => m.Duration),
                MinResponseTime = validMetrics.Min(m => m.Duration),
                OperationsPerSecond = validMetrics.Count / (period?.TotalSeconds ?? 300), // 默认5分钟
                SlowOperations = validMetrics.Count(m => m.Duration.TotalMilliseconds > 200), // >200ms
                OperationsByCollection = validMetrics
                    .GroupBy(m => m.CollectionName)
                    .ToDictionary(g => g.Key, g => g.Count()),
                OperationsByType = validMetrics
                    .GroupBy(m => m.OperationName)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Task.FromResult(metrics);
        }
    }

    internal void RecordOperation(string operationName, string collectionName, TimeSpan duration)
    {
        var metric = new OperationMetric
        {
            OperationName = operationName,
            CollectionName = collectionName,
            Duration = duration,
            Timestamp = DateTime.UtcNow
        };

        _metrics.Enqueue(metric);

        // 记录慢操作
        if (duration.TotalMilliseconds > 200)
            _logger.LogWarning("Slow MongoDB operation detected: {Operation} on {Collection} took {Duration}ms",
                operationName, collectionName, duration.TotalMilliseconds);

        // 记录非常慢的操作为错误
        if (duration.TotalMilliseconds > 1000)
            _logger.LogError("Very slow MongoDB operation: {Operation} on {Collection} took {Duration}ms",
                operationName, collectionName, duration.TotalMilliseconds);
    }

    private class OperationTimer : IDisposable
    {
        private readonly string _collectionName;
        private readonly ILogger _logger;
        private readonly MongoPerformanceMonitor _monitor;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        public OperationTimer(string operationName, string collectionName, MongoPerformanceMonitor monitor,
            ILogger logger)
        {
            _operationName = operationName;
            _collectionName = collectionName;
            _monitor = monitor;
            _logger = logger;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            if (_disposed) return;

            _stopwatch.Stop();
            _monitor.RecordOperation(_operationName, _collectionName, _stopwatch.Elapsed);
            _disposed = true;
        }
    }
}