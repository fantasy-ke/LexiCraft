using BuildingBlocks.Persistence.Abstractions.Transactions;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore.Transactions;

public class UnitOfWork<TDbContext>(TDbContext dbContext) : IUnitOfWork where TDbContext : DbContext
{
    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Database.CommitTransactionAsync(cancellationToken);
    }

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Database.RollbackTransactionAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(_ => action(), cancellationToken);
    }

    public Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(_ => action(), cancellationToken);
    }

    public void Dispose()
    {
        dbContext.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return dbContext.DisposeAsync();
    }
}
