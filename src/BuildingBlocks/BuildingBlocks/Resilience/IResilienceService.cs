namespace BuildingBlocks.Resilience;

/// <summary>
///     通用的弹性服务接口，提供重试、熔断等能力
/// </summary>
public interface IResilienceService
{
    /// <summary>
    ///     执行带重试的异步操作
    /// </summary>
    Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     执行带重试的异步操作（无返回值）
    /// </summary>
    Task ExecuteWithRetryAsync(
        Func<Task> operation,
        string operationName,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     检查服务健康状态
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}