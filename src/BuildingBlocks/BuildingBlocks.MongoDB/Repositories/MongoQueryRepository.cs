using System.Linq.Expressions;
using BuildingBlocks.MongoDB.Abstractions;
using BuildingBlocks.MongoDB.Entities;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Persistence.Abstractions.Repositories;
using BuildingBlocks.MongoDB.Resilience;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB.Repositories;

/// <summary>
///     MongoDB query repository with a single monitoring and read-resilience pipeline.
/// </summary>
public class MongoQueryRepository<TEntity> : IQueryRepository<TEntity>
    where TEntity : MongoEntity
{
    protected MongoQueryRepository(
        IMongoDbContext context,
        IMongoResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor,
        string? collectionName = null)
    {
        Context = context;
        ResilienceService = resilienceService;
        PerformanceMonitor = performanceMonitor;
        CollectionName = collectionName ?? typeof(TEntity).Name;
        Collection = context.Database.GetCollection<TEntity>(CollectionName);
    }

    public MongoQueryRepository(
        IMongoDbContext context,
        IMongoResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor)
        : this(context, resilienceService, performanceMonitor, null)
    {
    }

    protected IMongoDbContext Context { get; }
    protected IMongoResilienceService ResilienceService { get; }
    protected IMongoPerformanceMonitor PerformanceMonitor { get; }
    protected IMongoCollection<TEntity> Collection { get; }
    protected string CollectionName { get; }

    public virtual Task<List<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return FindAsync(predicate, cancellationToken);
    }

    public virtual Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return ExecuteFindAsync(
            "FirstOrDefault",
            predicate,
            query => query.Limit(1).FirstOrDefaultAsync(cancellationToken),
            cancellationToken)!;
    }

    public virtual Task<TEntity> FirstAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return ExecuteFindAsync(
            "First",
            predicate,
            query => query.Limit(1).FirstAsync(cancellationToken),
            cancellationToken);
    }

    public virtual Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return ExecuteFindAsync(
            "SingleOrDefault",
            predicate,
            query => query.Limit(2).SingleOrDefaultAsync(cancellationToken),
            cancellationToken)!;
    }

    public virtual Task<TEntity> SingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return ExecuteFindAsync(
            "Single",
            predicate,
            query => query.Limit(2).SingleAsync(cancellationToken),
            cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var count = await ExecuteReadOperationAsync(
            "Count",
            session => CountDocumentsAsync(predicate, session, cancellationToken),
            cancellationToken);
        return checked((int)count);
    }

    public virtual Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return ExecuteFindAsync(
            "Any",
            predicate,
            query => query.Limit(1).AnyAsync(cancellationToken),
            cancellationToken);
    }

    public virtual Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual Task<List<TEntity>> GetListAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteReadOperationAsync(
            "FindAll",
            session => Find(Builders<TEntity>.Filter.Empty, session).ToListAsync(cancellationToken),
            cancellationToken);
    }

    public virtual IQueryable<TEntity> Query()
    {
        return AsQueryable();
    }

    public virtual IQueryable<TEntity> QueryNoTracking()
    {
        return AsQueryable();
    }

    public virtual async Task<(int total, IEnumerable<TEntity> result)> GetPageListAsync(
        Expression<Func<TEntity, bool>> predicate,
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isAsc = true,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pageIndex, pageSize);
        var skip = checked((pageIndex - 1) * pageSize);
        var (items, totalCount) =
            await FindPagedAsync(predicate, skip, pageSize, orderBy, !isAsc, cancellationToken);
        return (checked((int)totalCount), items);
    }

    protected Task<List<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        return ExecuteFindAsync(
            "Find",
            filter,
            query => query.ToListAsync(cancellationToken),
            cancellationToken);
    }

    protected Task<(List<TEntity> Items, long TotalCount)> FindPagedAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        int skip = 0,
        int limit = 20,
        Expression<Func<TEntity, object>>? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
        if (limit <= 0) throw new ArgumentOutOfRangeException(nameof(limit));

        var filterDefinition = filter is null
            ? Builders<TEntity>.Filter.Empty
            : Builders<TEntity>.Filter.Where(filter);

        return ExecuteReadOperationAsync(
            "FindPaged",
            async session =>
            {
                var query = Find(filterDefinition, session);
                if (sortBy is not null)
                    query = sortDescending ? query.SortByDescending(sortBy) : query.SortBy(sortBy);

                if (session is not null)
                {
                    // MongoDB does not support parallel operations on the same transaction session.
                    var totalCount = await CountDocumentsAsync(filterDefinition, session, cancellationToken);
                    var items = await query.Skip(skip).Limit(limit).ToListAsync(cancellationToken);
                    return (items, totalCount);
                }

                var totalCountTask = CountDocumentsAsync(filterDefinition, null, cancellationToken);
                var itemsTask = query.Skip(skip).Limit(limit).ToListAsync(cancellationToken);
                await Task.WhenAll(totalCountTask, itemsTask);
                return (await itemsTask, await totalCountTask);
            },
            cancellationToken);
    }

    /// <summary>
    ///     Reads are retried only outside transactions. A transaction must be retried as one complete unit.
    /// </summary>
    protected async Task<TResult> ExecuteReadOperationAsync<TResult>(
        string operationName,
        Func<IClientSessionHandle?, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var _ = PerformanceMonitor.StartOperation(operationName, CollectionName);
        var session = GetActiveSession();

        if (session is not null)
            return await operation(session);

        return await ResilienceService.ExecuteWithRetryAsync(
            () => operation(null),
            $"{operationName}_{CollectionName}",
            cancellationToken);
    }

    /// <summary>
    ///     Writes rely on the MongoDB driver's retryable-write behavior and are not application-retried.
    /// </summary>
    protected async Task<TResult> ExecuteWriteOperationAsync<TResult>(
        string operationName,
        Func<IClientSessionHandle?, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var _ = PerformanceMonitor.StartOperation(operationName, CollectionName);
        return await operation(GetActiveSession());
    }

    protected async Task ExecuteWriteOperationAsync(
        string operationName,
        Func<IClientSessionHandle?, Task> operation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var _ = PerformanceMonitor.StartOperation(operationName, CollectionName);
        await operation(GetActiveSession());
    }

    private Task<TResult> ExecuteFindAsync<TResult>(
        string operationName,
        Expression<Func<TEntity, bool>> predicate,
        Func<IFindFluent<TEntity, TEntity>, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        return ExecuteReadOperationAsync(
            operationName,
            session => operation(Find(predicate, session)),
            cancellationToken);
    }

    private IFindFluent<TEntity, TEntity> Find(
        Expression<Func<TEntity, bool>> predicate,
        IClientSessionHandle? session)
    {
        return session is null ? Collection.Find(predicate) : Collection.Find(session, predicate);
    }

    private IFindFluent<TEntity, TEntity> Find(
        FilterDefinition<TEntity> filter,
        IClientSessionHandle? session)
    {
        return session is null ? Collection.Find(filter) : Collection.Find(session, filter);
    }

    private Task<long> CountDocumentsAsync(
        Expression<Func<TEntity, bool>> predicate,
        IClientSessionHandle? session,
        CancellationToken cancellationToken)
    {
        return session is null
            ? Collection.CountDocumentsAsync(predicate, cancellationToken: cancellationToken)
            : Collection.CountDocumentsAsync(session, predicate, cancellationToken: cancellationToken);
    }

    private Task<long> CountDocumentsAsync(
        FilterDefinition<TEntity> filter,
        IClientSessionHandle? session,
        CancellationToken cancellationToken)
    {
        return session is null
            ? Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken)
            : Collection.CountDocumentsAsync(session, filter, cancellationToken: cancellationToken);
    }

    private IQueryable<TEntity> AsQueryable()
    {
        var session = GetActiveSession();
        return session is null ? Collection.AsQueryable() : Collection.AsQueryable(session);
    }

    private IClientSessionHandle? GetActiveSession()
    {
        return Context.Session is { IsInTransaction: true } session ? session : null;
    }

    private static void ValidatePagination(int pageIndex, int pageSize)
    {
        if (pageIndex <= 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));
    }
}
