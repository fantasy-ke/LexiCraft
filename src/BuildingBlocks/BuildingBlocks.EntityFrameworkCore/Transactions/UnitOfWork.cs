using BuildingBlocks.Persistence.Abstractions.Transactions;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore.Transactions;

/// <summary>基于 EF Core 数据库上下文的工作单元实现。</summary>
/// <typeparam name="TDbContext">工作单元拥有的数据库上下文类型。</typeparam>
/// <param name="dbContext">当前作用域的数据库上下文。</param>
/// <remarks>
///     执行策略方法不会自动创建事务或保存变更。启用提供程序重试时，委托可能执行多次；
///     需要事务的调用方应把开始事务、业务写入、保存和提交放在同一次执行策略委托内。
/// </remarks>
public class UnitOfWork<TDbContext>(TDbContext dbContext) : IUnitOfWork where TDbContext : DbContext
{
    /// <inheritdoc />
    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Database.CommitTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Database.RollbackTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(_ => action(), cancellationToken);
    }

    /// <inheritdoc />
    public Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(_ => action(), cancellationToken);
    }

    /// <summary>同步释放工作单元拥有的数据库上下文。</summary>
    public void Dispose()
    {
        dbContext.Dispose();
    }

    /// <summary>异步释放工作单元拥有的数据库上下文。</summary>
    /// <returns>表示异步释放过程的值任务。</returns>
    public ValueTask DisposeAsync()
    {
        return dbContext.DisposeAsync();
    }
}
