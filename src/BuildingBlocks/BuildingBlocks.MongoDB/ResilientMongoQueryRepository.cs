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

    public virtual async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await FindAsync(predicate);
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
    {
        var results = await FindAsync(predicate);
        return results.FirstOrDefault();
    }

    public virtual async Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> predicate)
    {
        var results = await FindAsync(predicate);
        return results.First();
    }

    public virtual async Task<TEntity> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
    {
        var results = await FindAsync(predicate);
        return results.SingleOrDefault()!;
    }

    public virtual async Task<TEntity> SingleAsync(Expression<Func<TEntity, bool>> predicate)
    {
        var results = await FindAsync(predicate);
        return results.Single();
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate)
    {
        using var _ = PerformanceMonitor.StartOperation("Count", CollectionName);
        var count = await ResilienceService.ExecuteWithRetryAsync(
            async () => await Collection.CountDocumentsAsync(predicate),
            $"Count_{CollectionName}",
            CancellationToken.None);
        return (int)count;
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await CountAsync(predicate) > 0;
    }

    public virtual async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<List<TEntity>> GetListAsync()
    {
        return await FindAllAsync();
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
        Expression<Func<TEntity, bool>> predicate, int pageIndex, int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null, bool isAsc = true)
    {
        var (items, totalCount) =
            await FindPagedAsync(predicate, (pageIndex - 1) * pageSize, pageSize, orderBy, !isAsc);
        return ((int)totalCount, items);
    }

    public async Task<TEntity?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        using var _ = PerformanceMonitor.StartOperation("FindById", CollectionName);
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filter = Builders<TEntity>.Filter.Eq(x => x.Id, id);
                return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            },
            $"FindById_{CollectionName}",
            cancellationToken);
    }

    public async Task<List<TEntity>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        using var _ = PerformanceMonitor.StartOperation("FindAll", CollectionName);
        return await ResilienceService.ExecuteWithRetryAsync(
            async () => await Collection.Find(_ => true).ToListAsync(cancellationToken),
            $"FindAll_{CollectionName}",
            cancellationToken);
    }

    public async Task<List<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        using var _ = PerformanceMonitor.StartOperation("Find", CollectionName);
        return await ResilienceService.ExecuteWithRetryAsync(
            async () => await Collection.Find(filter).ToListAsync(cancellationToken),
            $"Find_{CollectionName}",
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
        using var _ = PerformanceMonitor.StartOperation("FindPaged", CollectionName);
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filterDefinition = filter != null
                    ? Builders<TEntity>.Filter.Where(filter)
                    : Builders<TEntity>.Filter.Empty;

                var query = Collection.Find(filterDefinition);

                if (sortBy != null)
                    query = sortDescending
                        ? query.SortByDescending(sortBy)
                        : query.SortBy(sortBy);

                var totalCountTask =
                    Collection.CountDocumentsAsync(filterDefinition, cancellationToken: cancellationToken);
                var itemsTask = query.Skip(skip).Limit(limit).ToListAsync(cancellationToken);

                await Task.WhenAll(totalCountTask, itemsTask);
                return (await itemsTask, await totalCountTask);
            },
            $"FindPaged_{CollectionName}",
            cancellationToken);
    }
}