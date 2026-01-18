using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BuildingBlocks.Caching.Abstractions;

/// <summary>
/// 缓存健康检查接口
/// </summary>
public interface ICacheHealthCheck
{
    /// <summary>
    /// 检查默认缓存实例的健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康检查结果</returns>
    Task<CacheHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查指定缓存实例的健康状态
    /// </summary>
    /// <param name="instanceName">实例名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康检查结果</returns>
    Task<CacheHealthResult> CheckHealthAsync(string instanceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查所有缓存实例的健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有实例的健康检查结果</returns>
    Task<Dictionary<string, CacheHealthResult>> CheckAllHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 缓存健康检查结果
/// </summary>
public class CacheHealthResult
{
    /// <summary>
    /// 是否健康
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// 额外数据
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// 创建健康的结果
    /// </summary>
    /// <param name="responseTimeMs">响应时间</param>
    /// <param name="data">额外数据</param>
    /// <returns>健康检查结果</returns>
    public static CacheHealthResult Healthy(long responseTimeMs = 0, Dictionary<string, object>? data = null)
    {
        return new CacheHealthResult
        {
            IsHealthy = true,
            ResponseTimeMs = responseTimeMs,
            Data = data ?? new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// 创建不健康的结果
    /// </summary>
    /// <param name="errorMessage">错误消息</param>
    /// <param name="responseTimeMs">响应时间</param>
    /// <param name="data">额外数据</param>
    /// <returns>健康检查结果</returns>
    public static CacheHealthResult Unhealthy(string errorMessage, long responseTimeMs = 0, Dictionary<string, object>? data = null)
    {
        return new CacheHealthResult
        {
            IsHealthy = false,
            ErrorMessage = errorMessage,
            ResponseTimeMs = responseTimeMs,
            Data = data ?? new Dictionary<string, object>()
        };
    }
}