using System.Linq.Expressions;
using BuildingBlocks.Abstractions;
using BuildingBlocks.Domain;
using BuildingBlocks.Domain.Internal;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB;

public class MongoRepository<TEntity>(IMongoDbContext context)
    : MongoQueryRepository<TEntity>(context), IRepository<TEntity>
    where TEntity : MongoEntity, IAggregateRoot
{
    public async Task<TEntity> InsertAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        if (Context.Session is { IsInTransaction: true } session)
            await Collection.InsertOneAsync(session, entity, cancellationToken: cancellationToken);
        else
            await Collection.InsertOneAsync(entity, cancellationToken: cancellationToken);

        return entity;
    }

    public async Task InsertAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();
        if (entityList.Count == 0) return;

        if (Context.Session is { IsInTransaction: true } session)
            await Collection.InsertManyAsync(session, entityList, cancellationToken: cancellationToken);
        else
            await Collection.InsertManyAsync(entityList, cancellationToken: cancellationToken);
    }

    public async Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);

        if (Context.Session is { IsInTransaction: true } session)
            await Collection.ReplaceOneAsync(session, filter, entity, cancellationToken: cancellationToken);
        else
            await Collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);

        return entity;
    }

    public async Task DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);

        if (Context.Session is { IsInTransaction: true } session)
            await Collection.DeleteOneAsync(session, filter, cancellationToken: cancellationToken);
        else
            await Collection.DeleteOneAsync(filter, cancellationToken);
    }

    public async Task DeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        if (Context.Session is { IsInTransaction: true } session)
            await Collection.DeleteManyAsync(session, predicate, cancellationToken: cancellationToken);
        else
            await Collection.DeleteManyAsync(predicate, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(0);
    }
}