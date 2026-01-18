using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Caching.Factories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BuildingBlocks.Caching.Services;

/// <summary>
/// 缓存健康检查实现
/// </summary>
public class CacheHealthCheck : ICacheHealthCheck
{
    private readonly IRedisConnectionFactory _connectionFactory;
    private readonly ILogger<CacheHealthCheck> _logger;

    /// <summary>
    /// 初始化缓存健康检查
    /// </summary>
    /// <param name="connectionFactory">Redis 连接工厂</param>
    /// <param name="logger">日志记录器</param>
    public CacheHealthCheck(
        IRedisConnectionFactory connectionFactory,
        ILogger<CacheHealthCheck> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 检查默认缓存实例的健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康检查结果</returns>
    public async Task<CacheHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        return await CheckHealthAsync("default", cancellationToken);
    }

    /// <summary>
    /// 检查指定缓存实例的健康状态
    /// </summary>
    /// <param name="instanceName">实例名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康检查结果</returns>
    public async Task<CacheHealthResult> CheckHealthAsync(string instanceName, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // 检查连接是否可用
            if (!_connectionFactory.IsConnected(instanceName))
            {
                return CacheHealthResult.Unhealthy(
                    $"Redis 实例 '{instanceName}' 未连接",
                    stopwatch.ElapsedMilliseconds);
            }

            // 获取数据库连接并执行 PING 命令
            var database = _connectionFactory.GetDatabase(instanceName);
            var pingResult = await database.PingAsync();

            stopwatch.Stop();

            var data = new Dictionary<string, object>
            {
                ["instance"] = instanceName,
                ["ping_time_ms"] = pingResult.TotalMilliseconds
            };

            _logger.LogDebug("Redis 实例 {InstanceName} 健康检查成功，响应时间: {ResponseTime}ms", 
                instanceName, stopwatch.ElapsedMilliseconds);

            return CacheHealthResult.Healthy(stopwatch.ElapsedMilliseconds, data);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "Redis 实例 {InstanceName} 健康检查失败", instanceName);

            return CacheHealthResult.Unhealthy(
                $"Redis 实例 '{instanceName}' 健康检查失败: {ex.Message}",
                stopwatch.ElapsedMilliseconds,
                new Dictionary<string, object>
                {
                    ["instance"] = instanceName,
                    ["exception"] = ex.GetType().Name
                });
        }
    }

    /// <summary>
    /// 检查所有缓存实例的健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有实例的健康检查结果</returns>
    public async Task<Dictionary<string, CacheHealthResult>> CheckAllHealthAsync(CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, CacheHealthResult>();
        
        try
        {
            // 获取所有连接信息
            var connectionInfos = _connectionFactory.GetAllConnectionInfo();
            
            if (!connectionInfos.Any())
            {
                // 如果没有连接信息，至少检查默认实例
                results["default"] = await CheckHealthAsync("default", cancellationToken);
                return results;
            }

            // 并行检查所有实例
            var tasks = connectionInfos.Select(async info =>
            {
                var result = await CheckHealthAsync(info.InstanceName, cancellationToken);
                return new { InstanceName = info.InstanceName, Result = result };
            });

            var completedTasks = await Task.WhenAll(tasks);

            foreach (var task in completedTasks)
            {
                results[task.InstanceName] = task.Result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查所有缓存实例健康状态时发生异常");
            
            // 如果出现异常，至少尝试检查默认实例
            if (!results.ContainsKey("default"))
            {
                results["default"] = CacheHealthResult.Unhealthy($"检查所有实例时发生异常: {ex.Message}");
            }
        }

        return results;
    }
}