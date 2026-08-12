namespace BuildingBlocks.Authentication.Contract;

/// <summary>
///     在多个 Identity API 实例之间串行化授权缓存变更。
/// </summary>
public interface IAuthorizationSynchronization
{
    /// <summary>
    ///     在指定授权资源的分布式互斥区内执行操作。
    /// </summary>
    /// <typeparam name="TResult">操作结果类型。</typeparam>
    /// <param name="resource">用于生成分布式锁键的资源标识。</param>
    /// <param name="action">获取锁后执行的异步操作。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步操作结果。</returns>
    Task<TResult> ExecuteAsync<TResult>(
        string resource,
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken = default);
}