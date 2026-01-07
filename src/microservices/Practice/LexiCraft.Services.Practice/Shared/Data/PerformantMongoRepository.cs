using BuildingBlocks.Abstractions;
using BuildingBlocks.Domain;
using BuildingBlocks.MongoDB;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Resilience;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace LexiCraft.Services.Practice.Shared.Data;

/// <summary>
/// Enhanced MongoDB repository with performance monitoring and resilience
/// </summary>
public class PerformantMongoRepository<TEntity> : ResilientMongoRepository<TEntity>, IRepository<TEntity>
    where TEntity : MongoEntity
{
    private readonly MongoRepository<TEntity> _baseRepository;
    private readonly IMongoDbContext _context;

    public PerformantMongoRepository(
        IMongoDbContext context,
        IResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor,
        ILogger<PerformantMongoRepository<TEntity>> logger)
        : base(context.Database, resilienceService, performanceMonitor, logger)
    {
        _context = context;
        _baseRepository = new MongoRepository<TEntity>(context);
    }

    // IRepository interface implementations using base resilient methods
    public async Task<TEntity> InsertAsync(TEntity entity)
    {
        return await base.InsertAsync(entity);
    }

    public async Task InsertAsync(IEnumerable<TEntity> entities)
    {
        await base.InsertManyAsync(entities);
    }

    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
    {
        var results = await base.FindAsync(predicate);
        return results.FirstOrDefault();
    }

    public async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await base.FindAsync(predicate);
    }

    public async Task<TEntity> UpdateAsync(TEntity entity)
    {
        var result = await base.UpdateAsync(entity);
        return result ?? throw new InvalidOperationException($"Failed to update entity with ID: {entity.Id}");
    }

    public async Task DeleteAsync(TEntity entity)
    {
        await base.DeleteAsync(entity.Id);
    }

    public async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate)
    {
        await base.DeleteManyAsync(predicate);
    }

    // Delegate other IRepository methods to base repository for compatibility
    public IQueryable<TTemp> Select<TTemp>() where TTemp : class => _baseRepository.Select<TTemp>();
    public Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> predicate) => _baseRepository.FirstAsync(predicate);
    public Task<TEntity> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate) => _baseRepository.SingleOrDefaultAsync(predicate);
    public Task<TEntity> SingleAsync(Expression<Func<TEntity, bool>> predicate) => _baseRepository.SingleAsync(predicate);
    public Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate) => _baseRepository.CountAsync(predicate);
    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate) => _baseRepository.AnyAsync(predicate);
    public Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate) => _baseRepository.GetAsync(predicate);
    public Task<List<TEntity>> GetListAsync() => _baseRepository.GetListAsync();
    public Task<int> SaveChangesAsync() => _baseRepository.SaveChangesAsync();
    public IQueryable<TEntity> QueryNoTracking() => _baseRepository.QueryNoTracking();
    public IQueryable<T> QueryNoTracking<T>() where T : class => _baseRepository.QueryNoTracking<T>();

    public async Task<(int total, IEnumerable<TEntity> result)> GetPageListAsync(
        Expression<Func<TEntity, bool>> predicate, int pageIndex, int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null, bool isAsc = true)
    {
        var (items, totalCount) = await base.FindPagedAsync(predicate, (pageIndex - 1) * pageSize, pageSize, orderBy, !isAsc);
        return ((int)totalCount, items);
    }

    /// <summary>
    /// Optimized paginated query with performance monitoring and resilience
    /// </summary>
    public async Task<(IEnumerable<TEntity> Items, long TotalCount)> GetPagedAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true,
        int page = 1,
        int pageSize = 20)
    {
        var skip = (page - 1) * pageSize;
        return await base.FindPagedAsync(filter, skip, pageSize, orderBy, !ascending);
    }

    /// <summary>
    /// Bulk update operation with performance monitoring and resilience
    /// </summary>
    public async Task<long> BulkUpdateAsync(
        Expression<Func<TEntity, bool>> filter,
        UpdateDefinition<TEntity> update)
    {
        using var monitor = PerformanceMonitor.StartOperation("BulkUpdate", CollectionName);
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filterDefinition = Builders<TEntity>.Filter.Where(filter);
                var result = await Collection.UpdateManyAsync(filterDefinition, update);
                
                Logger.LogInformation("Bulk update completed: {ModifiedCount} documents updated in {Collection}",
                    result.ModifiedCount, CollectionName);
                
                return result.ModifiedCount;
            },
            $"BulkUpdate_{CollectionName}");
    }

    /// <summary>
    /// Optimized aggregation pipeline with performance monitoring and resilience
    /// </summary>
    public async Task<IEnumerable<TResult>> AggregateAsync<TResult>(
        PipelineDefinition<TEntity, TResult> pipeline)
    {
        using var monitor = PerformanceMonitor.StartOperation("Aggregate", CollectionName);
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var options = new AggregateOptions
                {
                    AllowDiskUse = true, // Allow disk usage for large aggregations
                    MaxTime = TimeSpan.FromSeconds(30) // Set reasonable timeout
                };
                
                return await Collection.Aggregate(pipeline, options).ToListAsync();
            },
            $"Aggregate_{CollectionName}");
    }
}