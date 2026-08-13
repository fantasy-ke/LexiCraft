using System.Linq.Expressions;
using BuildingBlocks.Domain;
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Resilience;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB;

public class ResilientMongoRepository<TEntity> : ResilientMongoQueryRepository<TEntity>, IRepository<TEntity>
    where TEntity : MongoEntity, IAggregateRoot
{
    public ResilientMongoRepository(
        IMongoDatabase database,
        IResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor,
        ILogger<ResilientMongoRepository<TEntity>> logger,
        string? collectionName = null)
        : base(database, resilienceService, performanceMonitor, logger, collectionName)
    {
    }

    public virtual async Task<TEntity> InsertAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        using var _ = PerformanceMonitor.StartOperation("Insert", CollectionName);
        entity.CreationTime = DateTime.UtcNow;

        await ResilienceService.ExecuteWithRetryAsync(
            () => Collection.InsertOneAsync(entity, cancellationToken: cancellationToken),
            $"Insert_{CollectionName}",
            cancellationToken);
        return entity;
    }

    public virtual async Task InsertAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();
        if (entityList.Count == 0) return;

        using var _ = PerformanceMonitor.StartOperation("InsertMany", CollectionName);
        var now = DateTime.UtcNow;
        foreach (var entity in entityList) entity.CreationTime = now;

        await ResilienceService.ExecuteWithRetryAsync(
            () => Collection.InsertManyAsync(entityList, cancellationToken: cancellationToken),
            $"InsertMany_{CollectionName}",
            cancellationToken);
    }

    public virtual async Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        using var _ = PerformanceMonitor.StartOperation("Update", CollectionName);
        var filter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);
        var result = await ResilienceService.ExecuteWithRetryAsync(
            () => Collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken),
            $"Update_{CollectionName}",
            cancellationToken);

        return result.MatchedCount > 0
            ? entity
            : throw new InvalidOperationException($"Failed to update entity with ID: {entity.Id}");
    }

    public virtual async Task DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        using var _ = PerformanceMonitor.StartOperation("Delete", CollectionName);
        var filter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);
        await ResilienceService.ExecuteWithRetryAsync(
            () => Collection.DeleteOneAsync(filter, cancellationToken),
            $"Delete_{CollectionName}",
            cancellationToken);
    }

    public virtual async Task DeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        using var _ = PerformanceMonitor.StartOperation("DeleteMany", CollectionName);
        await ResilienceService.ExecuteWithRetryAsync(
            () => Collection.DeleteManyAsync(predicate, cancellationToken),
            $"DeleteMany_{CollectionName}",
            cancellationToken);
    }

    public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(0);
    }
}