namespace BuildingBlocks.Persistence.Abstractions.Transactions;

/// <summary>定义延迟写持久化提供程序的保存、事务与执行策略边界。</summary>
/// <remarks>
///     当前内置实现面向 EF Core。<see cref="ExecuteAsync(Func{Task}, CancellationToken)"/> 只调用提供程序执行策略，
///     不会自动开始事务、调用 <see cref="SaveChangesAsync"/> 或提交事务。
/// </remarks>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>异步开始数据库事务。</summary>
    /// <param name="cancellationToken">用于取消开始事务操作的令牌。</param>
    /// <returns>表示开始事务操作的任务。</returns>
    /// <exception cref="InvalidOperationException">提供程序不支持事务或当前上下文已有不兼容事务时实现可抛出。</exception>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>异步提交当前事务。</summary>
    /// <param name="cancellationToken">用于取消提交操作的令牌。</param>
    /// <returns>表示提交操作的任务。</returns>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>异步回滚当前事务。</summary>
    /// <param name="cancellationToken">用于取消回滚操作的令牌。</param>
    /// <returns>表示回滚操作的任务。</returns>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>异步保存当前工作单元中的待提交变更。</summary>
    /// <param name="cancellationToken">用于取消保存操作的令牌。</param>
    /// <returns>写入数据库的状态条目数。</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>使用数据库提供程序的执行策略执行异步操作。</summary>
    /// <param name="action">每次执行或重试时调用的异步委托。</param>
    /// <param name="cancellationToken">用于取消执行策略及后续重试的令牌。</param>
    /// <returns>表示执行过程的任务。</returns>
    /// <remarks>委托本身不接收令牌，调用方应在闭包内把同一令牌传给数据库 API。委托可能被执行多次，必须满足相应幂等或事务要求。</remarks>
    Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default);

    /// <summary>使用数据库提供程序的执行策略执行异步操作并返回结果。</summary>
    /// <typeparam name="TResult">操作结果类型。</typeparam>
    /// <param name="action">每次执行或重试时调用的异步委托。</param>
    /// <param name="cancellationToken">用于取消执行策略及后续重试的令牌。</param>
    /// <returns>异步操作的结果。</returns>
    /// <remarks>委托可能被执行多次；需要事务时应在执行策略委托内部创建并完成整个事务单元。</remarks>
    Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default);
}
