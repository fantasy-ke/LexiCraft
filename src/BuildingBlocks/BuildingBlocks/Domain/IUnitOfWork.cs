namespace BuildingBlocks.Domain;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default);

    Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default);
}
