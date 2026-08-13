using System.Linq.Expressions;
using BuildingBlocks.Domain;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Resilience;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB;

public class ResilientMongoQueryRepository<TEntity> : IQueryRepository<TEntity> where TEntity : MongoEntity
{
    protected readonly IMongoCollection<TEntity> Collection;
    protected readonly string CollectionName;
    protected readonly ILogger Logger;
    protected readonly IMongoPerformanceMonitor PerformanceMonitor;
    protected readonly IResilienceService ResilienceService;

    public ResilientMongoQueryRepository(
        IMongoDatabase database,
        IResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor,
        ILogger logger,
        string? collectionName = null)
    {
        CollectionName = collectionName ?? typeof(TEntity).Name.ToLowerInvariant();
        Collection = database.GetCollection<TEntity>(CollectionName);
        ResilienceService = resilienceService;
        PerformanceMonitor = performanceMonitor;
        Logger = logger;
    }

    public virtual IQueryable<TTemp> Select<TTemp>() where TTemp : class
    {
        throw new NotSupportedException(
            "MongoDB repository does not support arbitrary generic Select<T> directly like EF.");
    }

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
        using var _ = PerformanceMonitor.StartOperation("Count", CollectionName);
        var count = await ResilienceService.ExecuteWithRetryAsync(
            () => Collection.CountDocumentsAsync(predicate, cancellationToken: cancellationToken),
            $"Count_{CollectionName}",
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
        return FindAllAsync(cancellationToken);
    }

    public virtual IQueryable<TEntity> Query()
    {
        return Collection.AsQueryable();
    }

    public virtual IQueryable<TEntity> QueryNoTracking()
    {
        return Collection.AsQueryable();
    }

    public virtual IQueryable<T> QueryNoTracking<T>() where T : class
    {
        if (typeof(T) == typeof(TEntity)) return (IQueryable<T>)Collection.AsQueryable();

        throw new NotSupportedException(
            "QueryNoTracking with different type not fully supported in this generic adapter.");
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

    public Task<TEntity?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be null or whitespace.", nameof(id));

        if (!global::MongoDB.Bson.ObjectId.TryParse(id, out var objectId))
            throw new ArgumentException("Id is not a valid ObjectId.", nameof(id));

        return ExecuteFindAsync(
            "FindById",
            entity => entity.Id == objectId,
            query => query.Limit(1).FirstOrDefaultAsync(cancellationToken),
            cancellationToken)!;
    }

    public Task<List<TEntity>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteFindAsync(
            "FindAll",
            _ => true,
            query => query.ToListAsync(cancellationToken),
            cancellationToken);
    }

    public Task<List<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        return ExecuteFindAsync(
            "Find",
            filter,
            query => query.ToListAsync(cancellationToken),
            cancellationToken);
    }

    public async Task<(List<TEntity> Items, long TotalCount)> FindPagedAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        int skip = 0,
        int limit = 20,
        Expression<Func<TEntity, object>>? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
        if (limit <= 0) throw new ArgumentOutOfRangeException(nameof(limit));

        using var _ = PerformanceMonitor.StartOperation("FindPaged", CollectionName);
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filterDefinition = filter is null
                    ? Builders<TEntity>.Filter.Empty
                    : Builders<TEntity>.Filter.Where(filter);
                var query = Collection.Find(filterDefinition);

                if (sortBy is not null)
                    query = sortDescending ? query.SortByDescending(sortBy) : query.SortBy(sortBy);

                var totalCountTask =
                    Collection.CountDocumentsAsync(filterDefinition, cancellationToken: cancellationToken);
                var itemsTask = query.Skip(skip).Limit(limit).ToListAsync(cancellationToken);
                await Task.WhenAll(totalCountTask, itemsTask);
                return (await itemsTask, await totalCountTask);
            },
            $"FindPaged_{CollectionName}",
            cancellationToken);
    }

    private async Task<TResult> ExecuteFindAsync<TResult>(
        string operationName,
        Expression<Func<TEntity, bool>> predicate,
        Func<IFindFluent<TEntity, TEntity>, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        using var _ = PerformanceMonitor.StartOperation(operationName, CollectionName);
        return await ResilienceService.ExecuteWithRetryAsync(
            () => operation(Collection.Find(predicate)),
            $"{operationName}_{CollectionName}",
            cancellationToken);
    }

    private static void ValidatePagination(int pageIndex, int pageSize)
    {
        if (pageIndex <= 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));
    }
}