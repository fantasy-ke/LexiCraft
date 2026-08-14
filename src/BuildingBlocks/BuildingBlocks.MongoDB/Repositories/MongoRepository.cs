using System.Linq.Expressions;
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.MongoDB.Abstractions;
using BuildingBlocks.MongoDB.Entities;
using BuildingBlocks.MongoDB.Performance;
using BuildingBlocks.Persistence.Abstractions.Repositories;
using BuildingBlocks.MongoDB.Resilience;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB.Repositories;

public class MongoRepository<TEntity> : MongoQueryRepository<TEntity>, IRepository<TEntity>
    where TEntity : MongoEntity, IAggregateRoot
{
    protected MongoRepository(
        IMongoDbContext context,
        IMongoResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor,
        string? collectionName = null)
        : base(context, resilienceService, performanceMonitor, collectionName)
    {
    }

    public MongoRepository(
        IMongoDbContext context,
        IMongoResilienceService resilienceService,
        IMongoPerformanceMonitor performanceMonitor)
        : this(context, resilienceService, performanceMonitor, null)
    {
    }

    public virtual async Task<TEntity> InsertAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        entity.CreationTime = DateTime.UtcNow;
        await ExecuteWriteOperationAsync(
            "Insert",
            session => session is null
                ? Collection.InsertOneAsync(entity, cancellationToken: cancellationToken)
                : Collection.InsertOneAsync(session, entity, cancellationToken: cancellationToken),
            cancellationToken);
        return entity;
    }

    public virtual async Task InsertAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();
        if (entityList.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var entity in entityList) entity.CreationTime = now;

        await ExecuteWriteOperationAsync(
            "InsertMany",
            session => session is null
                ? Collection.InsertManyAsync(entityList, cancellationToken: cancellationToken)
                : Collection.InsertManyAsync(session, entityList, cancellationToken: cancellationToken),
            cancellationToken);
    }

    public virtual async Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);
        var result = await ExecuteWriteOperationAsync(
            "Update",
            session => session is null
                ? Collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken)
                : Collection.ReplaceOneAsync(session, filter, entity, cancellationToken: cancellationToken),
            cancellationToken);

        return result.MatchedCount > 0
            ? entity
            : throw new InvalidOperationException($"Failed to update entity with ID: {entity.Id}");
    }

    public virtual Task DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);
        return ExecuteWriteOperationAsync(
            "Delete",
            session => session is null
                ? Collection.DeleteOneAsync(filter, cancellationToken)
                : Collection.DeleteOneAsync(session, filter, cancellationToken: cancellationToken),
            cancellationToken);
    }

    public virtual Task DeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWriteOperationAsync(
            "DeleteMany",
            session => session is null
                ? Collection.DeleteManyAsync(predicate, cancellationToken)
                : Collection.DeleteManyAsync(session, predicate, cancellationToken: cancellationToken),
            cancellationToken);
    }

    public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(0);
    }
}
