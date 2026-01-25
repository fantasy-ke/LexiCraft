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
    public async Task<TEntity> InsertAsync(TEntity entity)
    {
        if (Context.Session != null && Context.Session.IsInTransaction)
            await Collection.InsertOneAsync(Context.Session, entity);
        else
            await Collection.InsertOneAsync(entity);

        return entity;
    }

    public async Task InsertAsync(IEnumerable<TEntity> entities)
    {
        if (Context.Session != null && Context.Session.IsInTransaction)
            await Collection.InsertManyAsync(Context.Session, entities);
        else
            await Collection.InsertManyAsync(entities);
    }

    public async Task<TEntity> UpdateAsync(TEntity entity)
    {
        var filter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);

        if (Context.Session != null && Context.Session.IsInTransaction)
            await Collection.ReplaceOneAsync(Context.Session, filter, entity);
        else
            await Collection.ReplaceOneAsync(filter, entity);

        return entity;
    }

    public async Task DeleteAsync(TEntity entity)
    {
        var filter = Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id);

        if (Context.Session != null && Context.Session.IsInTransaction)
            await Collection.DeleteOneAsync(Context.Session, filter);
        else
            await Collection.DeleteOneAsync(filter);
    }

    public async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate)
    {
        if (Context.Session is { IsInTransaction: true })
            await Collection.DeleteManyAsync(Context.Session, predicate);
        else
            await Collection.DeleteManyAsync(predicate);
    }

    public Task<int> SaveChangesAsync()
    {
        return Task.FromResult(1);
    }
}