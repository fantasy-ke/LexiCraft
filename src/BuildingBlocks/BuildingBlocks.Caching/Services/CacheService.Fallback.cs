using BuildingBlocks.Caching.Options;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Caching.Services;

internal sealed partial class CacheService
{
    /// <summary>
    ///     处理异常
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="ex">异常</param>
    /// <param name="options">配置选项</param>
    /// <param name="key">缓存键</param>
    /// <param name="operation">操作名称</param>
    /// <returns>默认值或异常处理结果</returns>
    private async Task<T?> HandleException<T>(Exception ex, CacheServiceOptions options, string key, string operation)
    {
        if (ex is OperationCanceledException)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();

        _logger.LogError(ex, "缓存操作异常: {Operation}, 键: {Key}, 异常类型: {ExceptionType}", operation, key, ex.GetType().Name);

        // 调用异常回调（需求 4.3: 异常回调机制）
        if (options.OnError != null)
            try
            {
                var callbackResult = options.OnError(ex);
                _logger.LogDebug("异常回调执行完成: {Operation}, 键: {Key}", operation, key);

                // 如果回调返回了合适的类型，直接返回
                if (callbackResult is T result) return result;

                // 如果回调返回了其他类型，尝试转换
                if (callbackResult != null && typeof(T).IsAssignableFrom(callbackResult.GetType()))
                    return (T)callbackResult;
            }
            catch (Exception callbackEx)
            {
                _logger.LogError(callbackEx, "异常回调执行失败: {Operation}, 键: {Key}", operation, key);

                // 如果异常回调本身失败且不隐藏异常，抛出原始异常
                if (!options.HideErrors)
                    throw new InvalidOperationException($"缓存操作失败且异常回调执行失败: {operation}, 键: {key}", ex);
            }

        // 需求 4.2: 透明异常模式 - 如果不隐藏异常，则重新抛出
        if (!options.HideErrors) throw new InvalidOperationException($"缓存操作失败: {operation}, 键: {key}", ex);

        // 需求 4.1: 异常隐藏模式 - 执行降级逻辑
        _logger.LogDebug("隐藏异常并执行降级逻辑: {Operation}, 键: {Key}", operation, key);

        // 尝试执行降级策略
        var fallbackResult = await ExecuteFallbackStrategy<T>(key, operation, options);
        if (fallbackResult.HasValue) return fallbackResult.Value;

        // 如果没有可用的降级策略，返回默认值
        return default;
    }

    /// <summary>
    ///     执行降级策略
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="operation">操作名称</param>
    /// <param name="options">配置选项</param>
    /// <returns>降级策略执行结果</returns>
    private async Task<(bool HasValue, T? Value)> ExecuteFallbackStrategy<T>(string key, string operation,
        CacheServiceOptions options)
    {
        // 需求 4.5: 默认值降级策略
        if (options.FallbackToDefault && options.DefaultValue != null)
            try
            {
                if (options.DefaultValue is T defaultValue)
                {
                    _logger.LogDebug("使用默认值降级策略: {Operation}, 键: {Key}", operation, key);
                    return (true, defaultValue);
                }

                // 尝试类型转换
                if (typeof(T).IsAssignableFrom(options.DefaultValue.GetType()))
                {
                    _logger.LogDebug("使用默认值降级策略 (类型转换): {Operation}, 键: {Key}", operation, key);
                    return (true, (T)options.DefaultValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "默认值降级策略执行失败: {Operation}, 键: {Key}", operation, key);
            }

        // 需求 4.6: 自定义函数降级策略
        if (options.FallbackFunction != null)
            try
            {
                var fallbackResult = options.FallbackFunction(key, operation);
                if (fallbackResult is T result)
                {
                    _logger.LogDebug("使用自定义函数降级策略: {Operation}, 键: {Key}", operation, key);
                    return (true, result);
                }

                // 尝试类型转换
                if (fallbackResult != null && typeof(T).IsAssignableFrom(fallbackResult.GetType()))
                {
                    _logger.LogDebug("使用自定义函数降级策略 (类型转换): {Operation}, 键: {Key}", operation, key);
                    return (true, (T)fallbackResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "自定义函数降级策略执行失败: {Operation}, 键: {Key}", operation, key);
            }

        return (false, default);
    }
}
