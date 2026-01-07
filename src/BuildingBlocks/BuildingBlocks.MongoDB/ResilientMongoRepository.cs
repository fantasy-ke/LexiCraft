using System.Linq.Expressions;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Resilience;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB;

/// <summary>
/// 具有弹性和性能监控的 MongoDB 仓储基类
/// </summary>
public abstract class ResilientMongoRepository<TEntity> where TEntity : MongoEntity
{
    protected readonly IMongoCollection<TEntity> Collection;
    protected readonly IResilienceService ResilienceService;
    protected readonly IMongoPerformanceMonitor PerformanceMonitor;
    protected readonly ILogger Logger;
    protected readonly string CollectionName;

    protected ResilientMongoRepository(
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

    /// <summary>
    /// 根据ID查找实体
    /// </summary>
    public virtual async Task<TEntity?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
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

    /// <summary>
    /// 查找所有实体
    /// </summary>
    public virtual async Task<List<TEntity>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        using var _ = PerformanceMonitor.StartOperation("FindAll", CollectionName);
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () => await Collection.Find(_ => true).ToListAsync(cancellationToken),
            $"FindAll_{CollectionName}",
            cancellationToken);
    }

    /// <summary>
    /// 根据条件查找实体
    /// </summary>
    public virtual async Task<List<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        using var _ = PerformanceMonitor.StartOperation("Find", CollectionName);
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () => await Collection.Find(filter).ToListAsync(cancellationToken),
            $"Find_{CollectionName}",
            cancellationToken);
    }

    /// <summary>
    /// 分页查询
    /// </summary>
    public virtual async Task<(List<TEntity> Items, long TotalCount)> FindPagedAsync(
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

                // 应用排序
                if (sortBy != null)
                {
                    query = sortDescending 
                        ? query.SortByDescending(sortBy)
                        : query.SortBy(sortBy);
                }

                // 获取总数和分页数据
                var totalCountTask = Collection.CountDocumentsAsync(filterDefinition, cancellationToken: cancellationToken);
                var itemsTask = query.Skip(skip).Limit(limit).ToListAsync(cancellationToken);

                await Task.WhenAll(totalCountTask, itemsTask);

                return (await itemsTask, await totalCountTask);
            },
            $"FindPaged_{CollectionName}",
            cancellationToken);
    }

    /// <summary>
    /// 插入实体
    /// </summary>
    public virtual async Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        using var _ = PerformanceMonitor.StartOperation("Insert", CollectionName);
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                entity.CreationTime = DateTime.UtcNow;
                
                await Collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
                return entity;
            },
            $"Insert_{CollectionName}",
            cancellationToken);
    }

    /// <summary>
    /// 批量插入
    /// </summary>
    public virtual async Task InsertManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        using var _ = PerformanceMonitor.StartOperation("InsertMany", CollectionName);
        
        await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var entitiesList = entities.ToList();
                var now = DateTime.UtcNow;
                
                foreach (var entity in entitiesList)
                {
                    entity.CreationTime = now;
                }
                
                await Collection.InsertManyAsync(entitiesList, cancellationToken: cancellationToken);
            },
            $"InsertMany_{CollectionName}",
            cancellationToken);
    }

    /// <summary>
    /// 更新实体
    /// </summary>
    public virtual async Task<TEntity?> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        using var _ = PerformanceMonitor.StartOperation("Update", CollectionName);
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);
                var result = await Collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
                
                return result.MatchedCount > 0 ? entity : null;
            },
            $"Update_{CollectionName}",
            cancellationToken);
    }

    /// <summary>
    /// 部分更新
    /// </summary>
    public virtual async Task<bool> UpdatePartialAsync(
        string id,
        UpdateDefinition<TEntity> updateDefinition,
        CancellationToken cancellationToken = default)
    {
        using var _ = PerformanceMonitor.StartOperation("UpdatePartial", CollectionName);
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filter = Builders<TEntity>.Filter.Eq(x => x.Id, id);
                var result = await Collection.UpdateOneAsync(filter, updateDefinition, cancellationToken: cancellationToken);
                return result.MatchedCount > 0;
            },
            $"UpdatePartial_{CollectionName}",
            cancellationToken);
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    public virtual async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using var _ = PerformanceMonitor.StartOperation("Delete", CollectionName);
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filter = Builders<TEntity>.Filter.Eq(x => x.Id, id);
                var result = await Collection.DeleteOneAsync(filter, cancellationToken);
                return result.DeletedCount > 0;
            },
            $"Delete_{CollectionName}",
            cancellationToken);
    }

    /// <summary>
    /// 批量删除
    /// </summary>
    public virtual async Task<long> DeleteManyAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        using var _ = PerformanceMonitor.StartOperation("DeleteMany", CollectionName);
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var result = await Collection.DeleteManyAsync(filter, cancellationToken);
                return result.DeletedCount;
            },
            $"DeleteMany_{CollectionName}",
            cancellationToken);
    }

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    public virtual async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        using var _ = PerformanceMonitor.StartOperation("Exists", CollectionName);
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filter = Builders<TEntity>.Filter.Eq(x => x.Id, id);
                var count = await Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
                return count > 0;
            },
            $"Exists_{CollectionName}",
            cancellationToken);
    }

    /// <summary>
    /// 获取集合统计信息
    /// </summary>
    public virtual async Task<long> CountAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        using var _ = PerformanceMonitor.StartOperation("Count", CollectionName);
        
        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filterDefinition = filter != null 
                    ? Builders<TEntity>.Filter.Where(filter)
                    : Builders<TEntity>.Filter.Empty;
                
                return await Collection.CountDocumentsAsync(filterDefinition, cancellationToken: cancellationToken);
            },
            $"Count_{CollectionName}",
            cancellationToken);
    }
}