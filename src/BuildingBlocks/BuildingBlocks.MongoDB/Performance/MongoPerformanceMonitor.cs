using System.Collections.Concurrent;
using System.Diagnostics;
using BuildingBlocks.MongoDB.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.MongoDB.Performance;

/// <summary>使用有界内存队列记录 MongoDB 仓储操作耗时并输出慢操作日志。</summary>
/// <remarks>
///     最多保留最近 10,000 条完成记录；超过 200 毫秒记 Warning，超过 1 秒记 Error。
///     该监控是进程内诊断工具，不提供跨实例聚合、持久化或分位数统计，不能替代 OpenTelemetry。
/// </remarks>
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

    /// <summary>创建 MongoDB 性能监控器。</summary>
    /// <param name="logger">慢操作日志记录器。</param>
    /// <param name="options">用于读取是否启用监控的 MongoDB 选项。</param>
    public MongoPerformanceMonitor(
        ILogger<MongoPerformanceMonitor> logger,
        IOptions<MongoOptions> options)
    {
        _logger = logger;
        _enabled = options.Value.EnablePerformanceMonitoring;
    }

    /// <inheritdoc />
    public IDisposable StartOperation(string operationName, string collectionName)
    {
        return _enabled
            ? new OperationTimer(operationName, collectionName, this)
            : EmptyDisposable.Instance;
    }

    /// <inheritdoc />
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

    /// <summary>记录完成的操作，并在并发安全的容量修剪后输出慢操作日志。</summary>
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