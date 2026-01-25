using System.Linq.Expressions;
using BuildingBlocks.Domain;
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Resilience;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB;

/// <summary>
///     具有弹性和性能监控的 MongoDB 仓储基类
/// </summary>
public class ResilientMongoRepository<TEntity> : ResilientMongoQueryRepository<TEntity>, IRepository<TEntity>
    where TEntity : MongoEntity, IAggregateRoot
{
    public ResilientMongoRepository(
        IMongoDatabase database,
        IResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor,
        ILogger logger,
        string? collectionName = null)
        : base(database, resilienceService, performanceMonitor, logger, collectionName)
    {
    }

    public virtual async Task<TEntity> InsertAsync(TEntity entity)
    {
        using var _ = PerformanceMonitor.StartOperation("Insert", CollectionName);

        return await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                entity.CreationTime = DateTime.UtcNow;
                await Collection.InsertOneAsync(entity);
                return entity;
            },
            $"Insert_{CollectionName}",
            CancellationToken.None);
    }

    public virtual async Task InsertAsync(IEnumerable<TEntity> entities)
    {
        using var _ = PerformanceMonitor.StartOperation("InsertMany", CollectionName);

        await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var entitiesList = entities.ToList();
                var now = DateTime.UtcNow;

                foreach (var entity in entitiesList) entity.CreationTime = now;

                await Collection.InsertManyAsync(entitiesList);
            },
            $"InsertMany_{CollectionName}",
            CancellationToken.None);
    }

    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        using var _ = PerformanceMonitor.StartOperation("Update", CollectionName);

        var result = await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);
                var replaceResult = await Collection.ReplaceOneAsync(filter, entity);
                return replaceResult.MatchedCount > 0 ? entity : null;
            },
            $"Update_{CollectionName}",
            CancellationToken.None);

        return result ?? throw new InvalidOperationException($"Failed to update entity with ID: {entity.Id}");
    }

    public virtual async Task DeleteAsync(TEntity entity)
    {
        using var _ = PerformanceMonitor.StartOperation("Delete", CollectionName);

        await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                var filter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);
                await Collection.DeleteOneAsync(filter);
                return true;
            },
            $"Delete_{CollectionName}",
            CancellationToken.None);
    }

    public virtual async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate)
    {
        using var _ = PerformanceMonitor.StartOperation("DeleteMany", CollectionName);

        await ResilienceService.ExecuteWithRetryAsync(
            async () =>
            {
                await Collection.DeleteManyAsync(predicate);
                return true;
            },
            $"DeleteMany_{CollectionName}",
            CancellationToken.None);
    }

    public virtual Task<int> SaveChangesAsync()
    {
        return Task.FromResult(1);
    }
}